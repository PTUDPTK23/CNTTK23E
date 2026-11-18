using System;

namespace EventFlow.Shared.Messaging;

public interface IEventMessage
{
    string MessageId { get; set; }
    Guid EventId { get; set; }
    string EventName { get; set; }
    DateTime Timestamp { get; set; }
    string CorrelationId { get; set; }
    string EventType { get; set; }
}
