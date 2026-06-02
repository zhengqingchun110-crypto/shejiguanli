namespace DecorationProjectScheduler.App.Models;

public sealed class ActiveProjectExportItem
{
    public ProjectSummary Project { get; init; } = new();
    public string ProjectDetail { get; init; } = string.Empty;
    public string TaskPlan { get; init; } = string.Empty;
    public List<ActiveProjectWorkItem> WorkItems { get; init; } = [];
}

public sealed class ActiveProjectWorkItem
{
    public string EmployeeName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string TaskName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime SubmissionDate { get; init; }
    public string DaysUntilSubmission { get; init; } = string.Empty;
}
