namespace MediQueue.Domain.Queues;

public enum QueueTicketStatus
{
    Waiting = 0,
    Serving = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}
