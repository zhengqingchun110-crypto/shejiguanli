namespace DecorationProjectScheduler.App.Models;

public sealed class DashboardCard
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public List<DashboardDetailGroup> Groups { get; init; } = [];
}

public sealed class DashboardDetailGroup
{
    public string Title { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public int TargetId { get; init; }
    public List<DashboardDetailItem> Items { get; init; } = [];
}

public sealed class DashboardDetailItem
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public int TargetId { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
}
