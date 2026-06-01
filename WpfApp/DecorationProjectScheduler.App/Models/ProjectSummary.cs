namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectSummary
{
    public int ProjectId { get; init; }
    public string ProjectCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string ManagerName { get; init; } = string.Empty;
    public string Area { get; init; } = string.Empty;
    public string ProjectType { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string OperatorNames { get; init; } = string.Empty;
    public string CurrentStage { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsDelayed { get; init; }
    public int TaskCount { get; init; }
}
