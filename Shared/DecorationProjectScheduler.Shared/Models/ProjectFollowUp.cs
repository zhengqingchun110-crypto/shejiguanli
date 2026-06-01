namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectFollowUp
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public int? TaskId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string OperatorName { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
}
