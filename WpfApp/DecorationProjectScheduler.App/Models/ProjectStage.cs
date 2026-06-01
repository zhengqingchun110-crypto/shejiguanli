namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectStage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int SequenceNo { get; set; }
    public DateTime PlannedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
