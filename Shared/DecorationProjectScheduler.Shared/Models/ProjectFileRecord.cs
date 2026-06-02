namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectFileRecord
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public string UploadedAtWithSizeText
    {
        get
        {
            var timeText = UploadedAt.ToString("yyyy-MM-dd HH:mm");
            return FileSizeBytes > 0 ? $"{timeText}  |  {FormatFileSize(FileSizeBytes)}" : timeText;
        }
    }

    public string UploadedAtText => $"上传时间：{UploadedAt:yyyy-MM-dd HH:mm}";

    public string FileSizeText => FileSizeBytes > 0
        ? $"文件大小：{FormatFileSize(FileSizeBytes)}"
        : "文件大小：云端待同步";

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{bytes} {units[unitIndex]}" : $"{size:0.##} {units[unitIndex]}";
    }
}
