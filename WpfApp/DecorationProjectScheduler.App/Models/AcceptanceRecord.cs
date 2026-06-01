namespace DecorationProjectScheduler.App.Models;

public sealed class AcceptanceRecord
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime AcceptanceDate { get; set; }
    public string Result { get; set; } = string.Empty;
    public string RectificationItems { get; set; } = string.Empty;
    public string ReviewRecord { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
