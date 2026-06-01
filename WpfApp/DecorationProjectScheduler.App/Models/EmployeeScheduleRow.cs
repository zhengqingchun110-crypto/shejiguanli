using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class EmployeeScheduleRow : ObservableObject
{
    [ObservableProperty]
    private int employeeId;

    [ObservableProperty]
    private int projectId;

    [ObservableProperty]
    private int? taskId;

    [ObservableProperty]
    private string employeeName = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private string projectName = string.Empty;

    [ObservableProperty]
    private string projectCode = string.Empty;

    [ObservableProperty]
    private string currentTask = string.Empty;

    [ObservableProperty]
    private DateTime submissionDate = DateTime.Today;

    [ObservableProperty]
    private string tagColor = "#D7E7FF";
}
