using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using EventFlow.EventApi.Data;
using EventFlow.EventApi.Entities;
using EventFlow.EventApi.Services;
using EventFlow.Shared.Messaging;
using EventFlow.Shared.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace EventFlow.Tests;

using ApiProgram = EventFlow.EventApi.Program;

public class EventApiIntegrationTests : IClassFixture<WebApplicationFactory<ApiProgram>>
{
    private readonly WebApplicationFactory<ApiProgram> _factory;
    private readonly Mock<IRabbitMqPublisher> _mockPublisher = new();
    private readonly string _testDbName = $"test_events_{Guid.NewGuid():N}.db";
    private readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public EventApiIntegrationTests(WebApplicationFactory<ApiProgram> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace DbContext with an isolated SQLite database for testing
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<EventFlowDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<EventFlowDbContext>(options =>
                {
                    options.UseSqlite($"Data Source={_testDbName}");
                });

                // Replace RabbitMQ Publisher with Mock
                var pubDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqPublisher));
                if (pubDescriptor != null) services.Remove(pubDescriptor);
                services.AddSingleton<IRabbitMqPublisher>(_mockPublisher.Object);
            });
        });
    }

    private async Task<HttpClient> CreateClientWithDbAsync()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventFlowDbContext>();
        await db.Database.EnsureCreatedAsync();
        await EventFlowDbContext.SeedAsync(db);
        return client;
    }

    [Fact]
    public async Task GetEvents_ReturnsSuccessAndSeededEvents()
    {
        var client = await CreateClientWithDbAsync();

        var response = await client.GetAsync("/events");
        response.EnsureSuccessStatusCode();

        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>(_jsonOptions);
        Assert.NotNull(events);
        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Name.Contains("Cloud & AI Summit"));
    }

    [Fact]
    public async Task RegisterParticipant_ValidRequest_CreatesRegistrationAndPublishesMessage()
    {
        var client = await CreateClientWithDbAsync();

        // 1. Get an existing event
        var events = await client.GetFromJsonAsync<List<EventDto>>("/events", _jsonOptions);
        Assert.NotNull(events);
        var targetEvent = events[0];

        // 2. Register
        var request = new RegisterParticipantRequest
        {
            FullName = "Nguyễn Văn Test",
            Email = $"test.{Guid.NewGuid():N}@example.com",
            PhoneNumber = "0987654321"
        };

        var postResponse = await client.PostAsJsonAsync($"/events/{targetEvent.Id}/registrations", request, _jsonOptions);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var regDetail = await postResponse.Content.ReadFromJsonAsync<RegistrationDetailDto>(_jsonOptions);
        Assert.NotNull(regDetail);
        Assert.False(string.IsNullOrWhiteSpace(regDetail.RegistrationCode));
        Assert.StartsWith("EVT-", regDetail.RegistrationCode);
        Assert.Equal(request.FullName, regDetail.ParticipantName);

        // Verify RabbitMQ message publish was invoked
        _mockPublisher.Verify(p => p.PublishAsync(
            RabbitMqConstants.RoutingKeyRegistered,
            It.Is<ParticipantRegisteredMessage>(m => m.RegistrationCode == regDetail.RegistrationCode)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task RegisterParticipant_DuplicateActiveRegistration_ReturnsConflict()
    {
        var client = await CreateClientWithDbAsync();
        var events = await client.GetFromJsonAsync<List<EventDto>>("/events", _jsonOptions);
        var targetEvent = events![0];

        var email = $"duplicate.{Guid.NewGuid():N}@example.com";
        var request = new RegisterParticipantRequest
        {
            FullName = "Lê Test",
            Email = email,
            PhoneNumber = "0911223344"
        };

        // First registration: OK
        var first = await client.PostAsJsonAsync($"/events/{targetEvent.Id}/registrations", request, _jsonOptions);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second registration with same email for same event: Conflict
        var second = await client.PostAsJsonAsync($"/events/{targetEvent.Id}/registrations", request, _jsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RegisterParticipant_WhenCapacityReached_ReturnsConflict_PreventsOverselling()
    {
        var client = await CreateClientWithDbAsync();

        // Create a custom event with capacity of 1
        var createEventReq = new CreateEventRequest
        {
            Name = "Tiny Capacity Workshop",
            Description = "Limited to 1 person",
            Location = "Online",
            StartTime = DateTime.UtcNow.AddDays(2),
            EndTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            MaxCapacity = 1
        };

        var createRes = await client.PostAsJsonAsync("/events", createEventReq, _jsonOptions);
        createRes.EnsureSuccessStatusCode();
        var tinyEvent = await createRes.Content.ReadFromJsonAsync<EventDto>(_jsonOptions);
        Assert.NotNull(tinyEvent);

        // 1st Registration: Success (1/1)
        var reg1 = new RegisterParticipantRequest { FullName = "First Attendee", Email = "first@tiny.com", PhoneNumber = "0111111" };
        var res1 = await client.PostAsJsonAsync($"/events/{tinyEvent.Id}/registrations", reg1, _jsonOptions);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        // 2nd Registration: Must fail with Conflict 409 (Capacity reached!)
        var reg2 = new RegisterParticipantRequest { FullName = "Second Attendee", Email = "second@tiny.com", PhoneNumber = "0222222" };
        var res2 = await client.PostAsJsonAsync($"/events/{tinyEvent.Id}/registrations", reg2, _jsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task CheckIn_ValidCode_Succeeds_AndSecondCheckIn_FailsWithConflict()
    {
        var client = await CreateClientWithDbAsync();
        var events = await client.GetFromJsonAsync<List<EventDto>>("/events", _jsonOptions);
        var targetEvent = events![0];

        // 1. Register attendee
        var regReq = new RegisterParticipantRequest
        {
            FullName = "Checkin Attendee",
            Email = $"checkin.{Guid.NewGuid():N}@example.com",
            PhoneNumber = "0999888777"
        };
        var regRes = await client.PostAsJsonAsync($"/events/{targetEvent.Id}/registrations", regReq, _jsonOptions);
        var reg = await regRes.Content.ReadFromJsonAsync<RegistrationDetailDto>(_jsonOptions);
        Assert.NotNull(reg);

        // 2. Perform Check-in: Success
        var checkInReq = new CheckInRequest { RegistrationCode = reg.RegistrationCode };
        var checkInRes1 = await client.PostAsJsonAsync("/checkin", checkInReq, _jsonOptions);
        Assert.Equal(HttpStatusCode.OK, checkInRes1.StatusCode);

        var result1 = await checkInRes1.Content.ReadFromJsonAsync<CheckInResultDto>(_jsonOptions);
        Assert.NotNull(result1);
        Assert.True(result1.Success);
        Assert.NotNull(result1.CheckedInAt);

        // Verify RabbitMQ publish
        _mockPublisher.Verify(p => p.PublishAsync(
            RabbitMqConstants.RoutingKeyCheckedIn,
            It.Is<ParticipantCheckedInMessage>(m => m.RegistrationCode == reg.RegistrationCode)),
            Times.AtLeastOnce);

        // 3. Second Check-in: Duplicate entry must be rejected with Conflict 409!
        var checkInRes2 = await client.PostAsJsonAsync("/checkin", checkInReq, _jsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, checkInRes2.StatusCode);

        var result2 = await checkInRes2.Content.ReadFromJsonAsync<CheckInResultDto>(_jsonOptions);
        Assert.NotNull(result2);
        Assert.False(result2.Success);
        Assert.Contains("trước đó", result2.Message);
    }

    [Fact]
    public async Task CancelRegistration_Valid_RestoresCapacityAndPublishesMessage()
    {
        var client = await CreateClientWithDbAsync();
        var events = await client.GetFromJsonAsync<List<EventDto>>("/events", _jsonOptions);
        var targetEvent = events![0];

        // 1. Register attendee
        var regReq = new RegisterParticipantRequest
        {
            FullName = "Cancel Attendee",
            Email = $"cancel.{Guid.NewGuid():N}@example.com",
            PhoneNumber = "0333444555"
        };
        var regRes = await client.PostAsJsonAsync($"/events/{targetEvent.Id}/registrations", regReq, _jsonOptions);
        var reg = await regRes.Content.ReadFromJsonAsync<RegistrationDetailDto>(_jsonOptions);
        Assert.NotNull(reg);

        // Check event registered count
        var evBeforeCancel = await client.GetFromJsonAsync<EventDto>($"/events/{targetEvent.Id}", _jsonOptions);
        var countBefore = evBeforeCancel!.RegisteredCount;

        // 2. Cancel registration
        var cancelRes = await client.DeleteAsync($"/registrations/{reg.Id}");
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        // Check event registered count decreased
        var evAfterCancel = await client.GetFromJsonAsync<EventDto>($"/events/{targetEvent.Id}", _jsonOptions);
        Assert.Equal(countBefore - 1, evAfterCancel!.RegisteredCount);

        // Verify RabbitMQ publish
        _mockPublisher.Verify(p => p.PublishAsync(
            RabbitMqConstants.RoutingKeyCancelled,
            It.Is<ParticipantCancelledMessage>(m => m.RegistrationCode == reg.RegistrationCode)),
            Times.AtLeastOnce);
    }
}
