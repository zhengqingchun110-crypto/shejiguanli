using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class NavigationMenuItem : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;
}
