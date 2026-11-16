using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using EventFlow.Shared.Messaging;
using Xunit;

namespace EventFlow.Tests;

public class MessageSerializationTests
{
    [Fact]
    public void ParticipantRegisteredMessage_Serialization_RoundTripsCorrectly()
    {
        var msg = new ParticipantRegisteredMessage
        {
            EventId = Guid.NewGuid(),
            EventName = "Cloud Native Conference",
            RegistrationId = Guid.NewGuid(),
            RegistrationCode = "EVT-998877",
            ParticipantId = Guid.NewGuid(),
            ParticipantName = "Nguyễn Văn Test",
            ParticipantEmail = "test@example.com",
            RemainingCapacity = 42,
            Timestamp = DateTime.UtcNow,
            CorrelationId = "corr-123"
        };

        var json = JsonSerializer.Serialize(msg);
        var deserialized = JsonSerializer.Deserialize<ParticipantRegisteredMessage>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(msg.EventId, deserialized.EventId);
        Assert.Equal(msg.EventName, deserialized.EventName);
        Assert.Equal(msg.RegistrationCode, deserialized.RegistrationCode);
        Assert.Equal(msg.ParticipantName, deserialized.ParticipantName);
        Assert.Equal(msg.RemainingCapacity, deserialized.RemainingCapacity);
        Assert.Equal(msg.CorrelationId, deserialized.CorrelationId);
        Assert.Equal("ParticipantRegistered", deserialized.EventType);
    }

    [Fact]
    public void ParticipantCancelledMessage_Serialization_RoundTripsCorrectly()
    {
        var msg = new ParticipantCancelledMessage
        {
            EventId = Guid.NewGuid(),
            EventName = ".NET Workshop",
            RegistrationId = Guid.NewGuid(),
            RegistrationCode = "EVT-112233",
            ParticipantId = Guid.NewGuid(),
            ParticipantName = "Trần Thị B",
            RemainingCapacity = 55,
            CorrelationId = "corr-cancel-1"
        };

        var json = JsonSerializer.Serialize(msg);
        var deserialized = JsonSerializer.Deserialize<ParticipantCancelledMessage>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(msg.RegistrationCode, deserialized.RegistrationCode);
        Assert.Equal("ParticipantCancelled", deserialized.EventType);
        Assert.Equal(55, deserialized.RemainingCapacity);
    }

    [Fact]
    public void ParticipantCheckedInMessage_Serialization_RoundTripsCorrectly()
    {
        var msg = new ParticipantCheckedInMessage
        {
            EventId = Guid.NewGuid(),
            EventName = "AI Summit",
            RegistrationId = Guid.NewGuid(),
            RegistrationCode = "EVT-445566",
            ParticipantName = "Lê Văn C",
            CheckedInAt = DateTime.UtcNow,
            CorrelationId = "corr-checkin-1"
        };

        var json = JsonSerializer.Serialize(msg);
        var deserialized = JsonSerializer.Deserialize<ParticipantCheckedInMessage>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(msg.RegistrationCode, deserialized.RegistrationCode);
        Assert.Equal("ParticipantCheckedIn", deserialized.EventType);
    }

    [Theory]
    [InlineData("event.participant.registered")]
    [InlineData("event.participant.cancelled")]
    [InlineData("event.participant.checkedin")]
    public void TopicPattern_MatchesAllEventTypes(string routingKey)
    {
        // RabbitMQ topic matching: "event.participant.*" matches one word after "event.participant."
        var regex = new Regex(@"^event\.participant\.[^\.]+$");
        Assert.Matches(regex, routingKey);
    }
}
