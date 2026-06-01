namespace DecorationProjectScheduler.App.Models;

public sealed class WorkTask
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public int WorkloadPercent { get; set; }
    public int ProgressPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
