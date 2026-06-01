using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class EmployeeDirectoryItem : ObservableObject
{
    [ObservableProperty]
    private int employeeId;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private string currentProject = "暂无项目";

    [ObservableProperty]
    private string currentTask = "待分配";

    [ObservableProperty]
    private DateTime submissionDate = DateTime.Today;
}
