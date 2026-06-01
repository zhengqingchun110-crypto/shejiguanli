using CommunityToolkit.Mvvm.ComponentModel;

namespace DecorationProjectScheduler.App.Models;

public partial class EmployeeProjectRow : ObservableObject
{
    [ObservableProperty]
    private ProjectOption? selectedProject;

    [ObservableProperty]
    private int projectId;

    [ObservableProperty]
    private int? taskId;

    [ObservableProperty]
    private string projectName = string.Empty;

    [ObservableProperty]
    private string projectCode = string.Empty;

    [ObservableProperty]
    private string currentTask = string.Empty;

    [ObservableProperty]
    private DateTime submissionDate = DateTime.Today;

    [ObservableProperty]
    private string tagColor = "#DCEBFF";

    public string DaysUntilSubmissionText
    {
        get
        {
            var days = (SubmissionDate.Date - DateTime.Today).Days;
            return days switch
            {
                > 0 => $"还有 {days} 天",
                0 => "今天提交",
                _ => $"已延期 {Math.Abs(days)} 天"
            };
        }
    }

    partial void OnSubmissionDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(DaysUntilSubmissionText));
    }

    partial void OnSelectedProjectChanged(ProjectOption? value)
    {
        if (value is null)
        {
            return;
        }

        ProjectId = value.ProjectId;
        ProjectName = value.Name;
        ProjectCode = value.ProjectCode;
        TagColor = value.TagColor;
    }
}
