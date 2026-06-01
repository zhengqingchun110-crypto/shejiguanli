namespace DecorationProjectScheduler.App.Models;

public sealed class StaffTaskItem
{
    public string ProjectName { get; init; } = string.Empty;
    public string TaskName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public int WorkloadPercent { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}
