namespace EventContracts;
public sealed record BusinessEvent(Guid EventId,string EventType,DateTimeOffset OccurredAt,string CorrelationId,int EventVersion,EventData EventData);
public sealed record EventData(Guid? RegistrationId,string? RegistrationCode,Guid? ParticipantId,string? ParticipantName,string? Email,Guid EventEntityId,string? EventName,DateTimeOffset? StartsAt,int Capacity,int RegisteredCount,int CheckedInCount,string? Description=null);
