namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectTaskDisplayItem
{
    public int TaskId { get; init; }
    public string TaskName { get; init; } = string.Empty;
    public string OwnerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public DateTime EndDate { get; init; }
}
