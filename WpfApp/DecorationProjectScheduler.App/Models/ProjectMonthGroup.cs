using System.Collections.ObjectModel;

namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectMonthGroup
{
    public int Month { get; init; }
    public string Title => $"{Month:00} 月";
    public ObservableCollection<ProjectSummary> Projects { get; init; } = [];
}
