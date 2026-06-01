namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectDetailSnapshot
{
    public Project? Project { get; init; }
    public Employee? Manager { get; init; }
    public List<ProjectStage> Stages { get; init; } = [];
    public List<WorkTask> Tasks { get; init; } = [];
    public List<SiteVisit> SiteVisits { get; init; } = [];
    public List<HandoverRecord> HandoverRecords { get; init; } = [];
    public List<AcceptanceRecord> AcceptanceRecords { get; init; } = [];
    public List<ProjectFileRecord> ProjectFiles { get; init; } = [];
}
