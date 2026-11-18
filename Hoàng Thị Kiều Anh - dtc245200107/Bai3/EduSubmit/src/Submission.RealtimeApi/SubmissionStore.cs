sealed class SubmissionStore
{
    private readonly List<SubmissionDto> submissions = [];
    private readonly object gate = new();

    public SubmissionDto? Create(int assignmentId, string studentCode, string repositoryUrl)
    {
        lock (gate)
        {
            if (submissions.Exists(s => s.AssignmentId == assignmentId &&
                string.Equals(s.StudentCode, studentCode, StringComparison.OrdinalIgnoreCase)))
                return null;

            var now = DateTimeOffset.UtcNow;
            var submission = new SubmissionDto(
                Guid.NewGuid(), assignmentId, studentCode, repositoryUrl, "received", now, now);
            submissions.Add(submission);
            return submission;
        }
    }

    public SubmissionDto? Find(Guid id)
    {
        lock (gate)
            return submissions.Find(s => s.Id == id);
    }

    public bool Remove(Guid id)
    {
        lock (gate)
            return submissions.RemoveAll(s => s.Id == id) > 0;
    }

    public SubmissionDto? UpdateStatus(Guid id, string status)
    {
        lock (gate)
        {
            var index = submissions.FindIndex(s => s.Id == id);
            if (index < 0)
                return null;

            var updated = submissions[index] with
            {
                Status = status,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            submissions[index] = updated;
            return updated;
        }
    }
}
