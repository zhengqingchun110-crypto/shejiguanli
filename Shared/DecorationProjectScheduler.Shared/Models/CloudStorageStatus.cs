namespace DecorationProjectScheduler.App.Models;

public sealed class CloudStorageStatus
{
    public bool IsCloud { get; set; }
    public long AvailableBytes { get; set; }
    public long TotalBytes { get; set; }
    public DateTime CheckedAt { get; set; }
}
