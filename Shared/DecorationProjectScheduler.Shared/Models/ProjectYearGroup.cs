using System.Collections.ObjectModel;

namespace DecorationProjectScheduler.App.Models;

public sealed class ProjectYearGroup
{
    public int Year { get; init; }
    public string Title => $"{Year} 年";
    public ObservableCollection<ProjectMonthGroup> Months { get; init; } = [];
}
