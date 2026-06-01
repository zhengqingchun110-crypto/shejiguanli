using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class DepartmentWorkPage : ObservableObject
{
    [ObservableProperty]
    private string departmentName = string.Empty;

    [ObservableProperty]
    private string newEmployeeName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<EmployeeWorkGroup> employeeGroups = [];

    public override string ToString() => DepartmentName;
}
