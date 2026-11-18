namespace Shared;

public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class Student
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}

public class Submission
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; }
    public string Status { get; set; } = "Submitted";
    public double? Score { get; set; }
    public string? Comment { get; set; }
}
