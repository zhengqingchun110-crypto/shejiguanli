namespace DecorationProjectScheduler.App.Models;

public sealed class WorkspaceSnapshot
{
    public List<Employee> Employees { get; init; } = [];
    public List<Project> Projects { get; init; } = [];
    public List<ProjectStage> ProjectStages { get; init; } = [];
    public List<WorkTask> Tasks { get; init; } = [];
    public List<SiteVisit> SiteVisits { get; init; } = [];
    public List<HandoverRecord> HandoverRecords { get; init; } = [];
    public List<AcceptanceRecord> AcceptanceRecords { get; init; } = [];
    public List<ProjectFileRecord> ProjectFiles { get; init; } = [];
    public List<ProjectFollowUp> ProjectFollowUps { get; init; } = [];
}
