namespace FileProcessing.Core.Enums;

public enum OutboxStatus
{
    Pending,
    InFlight,
    Processed,
    Failed
}

