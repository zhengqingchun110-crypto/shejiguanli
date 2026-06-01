namespace DecorationProjectScheduler.App.Models;

public sealed class TimelineItem
{
    public string ProjectName { get; init; } = string.Empty;
    public string StageName { get; init; } = string.Empty;
    public DateTime PlannedDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsDelayed { get; init; }
}
