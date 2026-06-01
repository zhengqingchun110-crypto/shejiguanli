namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectFileRecord
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}
