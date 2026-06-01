namespace DecorationProjectScheduler.App.Models;

public sealed class Project
{
    public int Id { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ManagerId { get; set; }
    public string Area { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string OperatorNames { get; set; } = string.Empty;
    public string TaskPlan { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Summary { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public bool Archived { get; set; }
}
