namespace DecorationProjectScheduler.App.Models;

public sealed class RecentProjectUpdate
{
    public string ProjectName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}
