using System;
using System.Threading.Tasks;
using EventFlow.DashboardService.Hubs;
using EventFlow.DashboardService.Services;
using EventFlow.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace EventFlow.Tests;

public class DashboardServiceTests
{
    private readonly Mock<IHubContext<DashboardHub>> _mockHubContext;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly DashboardStateService _service;

    public DashboardServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<DashboardHub>>();
        _mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

        var config = new ConfigurationBuilder().Build();
        var clientFactory = new Mock<System.Net.Http.IHttpClientFactory>();

        _service = new DashboardStateService(
            _mockHubContext.Object,
            clientFactory.Object,
            config,
            NullLogger<DashboardStateService>.Instance);
    }

    [Fact]
    public async Task HandleParticipantRegistered_IncrementsRegistrations_AndRecalculatesStats()
    {
        var initial = _service.GetCurrentStats();
        var initialReg = initial.TotalRegistrations;
        var initialSpots = initial.TotalRemainingSpots;

        var msg = new ParticipantRegisteredMessage
        {
            EventId = Guid.NewGuid(),
            EventName = "Tech Expo",
            RegistrationCode = "EVT-888888",
            ParticipantName = "Trần A",
            RemainingCapacity = 99
        };

        await _service.HandleParticipantRegisteredAsync(msg);

        var updated = _service.GetCurrentStats();
        Assert.Equal(initialReg + 1, updated.TotalRegistrations);
        Assert.Equal(initialSpots - 1, updated.TotalRemainingSpots);
        Assert.NotEmpty(updated.RecentActivities);
        Assert.Equal("ParticipantRegistered", updated.RecentActivities[0].EventType);
    }

    [Fact]
    public async Task HandleParticipantCheckedIn_IncrementsCheckedIn_AndUpdatesRate()
    {
        // First register 2 attendees
        await _service.HandleParticipantRegisteredAsync(new ParticipantRegisteredMessage { ParticipantName = "User 1" });
        await _service.HandleParticipantRegisteredAsync(new ParticipantRegisteredMessage { ParticipantName = "User 2" });

        var preCheckin = _service.GetCurrentStats();
        var initialCheckedIn = preCheckin.TotalCheckedIn;

        await _service.HandleParticipantCheckedInAsync(new ParticipantCheckedInMessage
        {
            EventName = "Tech Expo",
            RegistrationCode = "EVT-888888",
            ParticipantName = "User 1"
        });

        var updated = _service.GetCurrentStats();
        Assert.Equal(initialCheckedIn + 1, updated.TotalCheckedIn);
        Assert.True(updated.OverallCheckInRate > 0);
    }

    [Fact]
    public async Task HandleParticipantCancelled_DecrementsRegistrations_AndRestoresSpots()
    {
        // Register first
        await _service.HandleParticipantRegisteredAsync(new ParticipantRegisteredMessage { RegistrationCode = "EVT-555" });

        var afterReg = _service.GetCurrentStats();

        // Cancel
        await _service.HandleParticipantCancelledAsync(new ParticipantCancelledMessage
        {
            RegistrationCode = "EVT-555",
            ParticipantName = "User Cancelled"
        });

        var afterCancel = _service.GetCurrentStats();
        Assert.Equal(afterReg.TotalRegistrations - 1, afterCancel.TotalRegistrations);
        Assert.Equal(afterReg.TotalRemainingSpots + 1, afterCancel.TotalRemainingSpots);
    }
}
