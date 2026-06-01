namespace DecorationProjectScheduler.App.Models;

public sealed class StaffWorkbenchItem
{
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string CurrentTask { get; init; } = "待分配";
    public string CurrentProject { get; init; } = "暂无项目";
    public string CurrentStage { get; init; } = "未开始";
    public int DelayedTaskCount { get; init; }
    public List<StaffTaskItem> Tasks { get; init; } = [];
}
