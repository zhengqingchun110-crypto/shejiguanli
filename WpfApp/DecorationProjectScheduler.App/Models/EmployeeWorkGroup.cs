using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class EmployeeWorkGroup : ObservableObject
{
    [ObservableProperty]
    private int employeeId;

    [ObservableProperty]
    private string employeeName = string.Empty;

    [ObservableProperty]
    private string role = string.Empty;

    [ObservableProperty]
    private ObservableCollection<EmployeeProjectRow> projectRows = [];
}
