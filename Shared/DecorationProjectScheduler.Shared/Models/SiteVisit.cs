namespace DecorationProjectScheduler.App.Models;

public sealed class SiteVisit
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime VisitDate { get; set; }
    public string Issues { get; set; } = string.Empty;
    public string Suggestions { get; set; } = string.Empty;
    public string PhotoPath { get; set; } = string.Empty;
    public string RectificationStatus { get; set; } = string.Empty;
}
