namespace DecorationProjectScheduler.App.Models;

public sealed class HandoverRecord
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public DateTime HandoverDate { get; set; }
    public string Participants { get; set; } = string.Empty;
    public string AttachmentPath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
