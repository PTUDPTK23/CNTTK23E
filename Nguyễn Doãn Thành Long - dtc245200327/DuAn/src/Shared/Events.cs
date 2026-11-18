namespace Shared;

public static class RabbitMqQueues
{
    public const string SubmissionGrading = "submission.grading";
    public const string SubmissionGraded = "submission.graded";
}

public record SubmissionGradingMessage(
    int SubmissionId,
    int AssignmentId,
    string StudentId,
    string Content,
    DateTimeOffset SubmittedAt);

public record SubmissionGradedEvent(
    int SubmissionId,
    string StudentId,
    string Status,
    double Score,
    string Comment);
