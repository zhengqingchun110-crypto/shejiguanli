using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class ResourceCategorySection : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string newCloudTitle = string.Empty;

    [ObservableProperty]
    private string newCloudUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ProjectFileRecord> localFiles = [];

    [ObservableProperty]
    private ObservableCollection<ProjectFileRecord> cloudLinks = [];

    [ObservableProperty]
    private ObservableCollection<SiteVisit> siteVisits = [];

    public string LocalEmptyText => LocalFiles.Count == 0 ? "空" : string.Empty;

    public string CloudEmptyText => CloudLinks.Count == 0 ? "空" : string.Empty;

    public string SiteVisitEmptyText => SiteVisits.Count == 0 ? "空" : string.Empty;

    public override string ToString() => Name;
}
