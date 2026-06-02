using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Media;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DecorationProjectScheduler.App.Models;
using DecorationProjectScheduler.App.Repositories;
using DecorationProjectScheduler.App.Services;

namespace DecorationProjectScheduler.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly ISchedulerRepository _repository;
    private readonly UpdateService _updateService;
    private static readonly string[] DepartmentNames = ["空间部门", "策划部门", "平面部门", "施工图部门", "工程监理"];

    public MainViewModel(ISchedulerRepository repository, ThemeService themeService, UpdateService updateService)
    {
        _repository = repository;
        _updateService = updateService;

        NavigationItems =
        [
            new NavigationMenuItem { Title = "总览" },
            new NavigationMenuItem { Title = "人员管理" },
            new NavigationMenuItem { Title = "项目中心" }
        ];

        FilterStatuses =
        [
            "全部状态",
            "规划中",
            "方案中",
            "执行中",
            "巡场中",
            "已归档"
        ];

        TaskStatuses =
        [
            "待开始",
            "进行中",
            "待验收",
            "已完成"
        ];

        VisitStatuses =
        [
            "待整改",
            "整改中",
            "待复验",
            "已关闭"
        ];

        AcceptanceStatuses =
        [
            "待复验",
            "复验中",
            "已通过"
        ];

        FileCategories =
        [
            "图纸文件",
            "模型文件",
            "效果图",
            "施工图",
            "巡场照片",
            "验收资料"
        ];

        SelectedNavigation = NavigationItems[0];
        SelectedProjectStatus = FilterStatuses[0];
        SelectedTaskStatus = TaskStatuses[1];
        SelectedVisitStatus = VisitStatuses[1];
        SelectedAcceptanceStatus = AcceptanceStatuses[0];
        SelectedFileCategory = FileCategories[0];

        _repository.DataChanged += (_, _) => Reload();
        Reload();
    }

    public ObservableCollection<NavigationMenuItem> NavigationItems { get; }
    public ObservableCollection<DashboardCard> DashboardCards { get; } = [];
    public ObservableCollection<RecentProjectUpdate> RecentUpdates { get; } = [];
    public ObservableCollection<EmployeeWorkGroup> EmployeeGroups { get; } = [];
    public ObservableCollection<DepartmentWorkPage> DepartmentPages { get; } = [];
    public ObservableCollection<ProjectOption> ProjectOptions { get; } = [];
    public ObservableCollection<ProjectSummary> ProjectCenterItems { get; } = [];
    public ObservableCollection<ProjectYearGroup> ProjectTreeGroups { get; } = [];
    public ObservableCollection<ProjectStage> SelectedProjectStages { get; } = [];
    public ObservableCollection<ProjectTaskDisplayItem> SelectedProjectTasks { get; } = [];
    public ObservableCollection<SiteVisit> SelectedSiteVisits { get; } = [];
    public ObservableCollection<AcceptanceRecord> SelectedAcceptanceRecords { get; } = [];
    public ObservableCollection<ProjectFileRecord> SelectedProjectFiles { get; } = [];
    public ObservableCollection<ProjectFollowUp> SelectedProjectFollowUps { get; } = [];
    public ObservableCollection<TimelineItem> AllTimelineItems { get; } = [];
    public ObservableCollection<Employee> Managers { get; } = [];
    public ObservableCollection<Employee> Employees { get; } = [];
    public ObservableCollection<string> FilterStatuses { get; }
    public ObservableCollection<string> TaskStatuses { get; }
    public ObservableCollection<string> VisitStatuses { get; }
    public ObservableCollection<string> AcceptanceStatuses { get; }
    public ObservableCollection<string> FileCategories { get; }

    [ObservableProperty]
    private NavigationMenuItem? selectedNavigation;

    [ObservableProperty]
    private string selectedProjectStatus;

    [ObservableProperty]
    private string projectSearchKeyword = string.Empty;

    [ObservableProperty]
    private ProjectSummary? selectedProjectSummary;

    [ObservableProperty]
    private string newEmployeeName = string.Empty;

    [ObservableProperty]
    private string newEmployeeRole = string.Empty;

    [ObservableProperty]
    private DepartmentWorkPage? selectedDepartmentPage;

    [ObservableProperty]
    private string newProjectName = string.Empty;

    [ObservableProperty]
    private string newProjectArea = string.Empty;

    [ObservableProperty]
    private string newProjectType = string.Empty;

    [ObservableProperty]
    private Employee? selectedManager;

    [ObservableProperty]
    private DateTime newProjectStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime newProjectEndDate = DateTime.Today.AddDays(45);

    [ObservableProperty]
    private string newProjectSummary = "新项目待补充设计任务、交付节点和资料要求。";

    [ObservableProperty]
    private string newTaskName = string.Empty;

    [ObservableProperty]
    private string editableProjectName = string.Empty;

    [ObservableProperty]
    private string editableProjectArea = string.Empty;

    [ObservableProperty]
    private string editableProjectType = string.Empty;

    [ObservableProperty]
    private string editableProjectOperators = string.Empty;

    [ObservableProperty]
    private string editableProjectSummary = string.Empty;

    [ObservableProperty]
    private string editableTaskPlanText = string.Empty;

    [ObservableProperty]
    private Employee? selectedTaskOwner;

    [ObservableProperty]
    private int newTaskProgress;

    [ObservableProperty]
    private DateTime newTaskStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime newTaskEndDate = DateTime.Today.AddDays(5);

    [ObservableProperty]
    private string selectedTaskStatus;

    [ObservableProperty]
    private string newVisitIssues = string.Empty;

    [ObservableProperty]
    private string newVisitSuggestions = string.Empty;

    [ObservableProperty]
    private DateTime newVisitDate = DateTime.Today;

    [ObservableProperty]
    private string selectedVisitStatus;

    [ObservableProperty]
    private string newAcceptanceResult = string.Empty;

    [ObservableProperty]
    private string newAcceptanceRectificationItems = string.Empty;

    [ObservableProperty]
    private string newAcceptanceReviewRecord = string.Empty;

    [ObservableProperty]
    private DateTime newAcceptanceDate = DateTime.Today;

    [ObservableProperty]
    private string selectedAcceptanceStatus;

    [ObservableProperty]
    private string selectedFileCategory;

    [ObservableProperty]
    private string newFileName = string.Empty;

    [ObservableProperty]
    private string newFilePath = string.Empty;

    [ObservableProperty]
    private bool hasPendingChanges;

    [ObservableProperty]
    private bool isOnline;

    [ObservableProperty]
    private string connectionStatusText = "检测中";

    [ObservableProperty]
    private string lastSyncText = "尚未同步";

    [ObservableProperty]
    private string updateStatusText = "当前版本";

    [ObservableProperty]
    private DashboardCard? expandedDashboardCard;

    public Brush ConnectionIndicatorBrush => IsOnline
        ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
        : new SolidColorBrush(Color.FromRgb(148, 163, 184));

    public Visibility OverviewVisibility => IsMenu("总览") ? Visibility.Visible : Visibility.Collapsed;
    public Visibility PeopleVisibility => IsMenu("人员管理") ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ProjectCenterVisibility => IsMenu("项目中心") ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NonProjectContentVisibility => IsMenu("项目中心") ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ProjectDetailVisibility => Visibility.Collapsed;
    public Visibility TimelineVisibility => Visibility.Collapsed;

    partial void OnSelectedProjectStatusChanged(string value) => ApplyProjectFilters();
    partial void OnProjectSearchKeywordChanged(string value) => ApplyProjectFilters();
    partial void OnSelectedProjectSummaryChanged(ProjectSummary? value) => LoadProjectDetail();
    partial void OnExpandedDashboardCardChanged(DashboardCard? value) => OnPropertyChanged(nameof(DashboardDetailVisibility));

    public Visibility DashboardDetailVisibility => IsMenu("总览") && ExpandedDashboardCard is not null ? Visibility.Visible : Visibility.Collapsed;

    partial void OnSelectedNavigationChanged(NavigationMenuItem? value)
    {
        OnPropertyChanged(nameof(OverviewVisibility));
        OnPropertyChanged(nameof(PeopleVisibility));
        OnPropertyChanged(nameof(ProjectCenterVisibility));
        OnPropertyChanged(nameof(NonProjectContentVisibility));
        OnPropertyChanged(nameof(DashboardDetailVisibility));
    }

    [RelayCommand]
    private void Reload()
    {
        try
        {
            var snapshot = _repository.GetSnapshot();
            RefreshCollections(snapshot);
            UpdateConnectionStatus(true);
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus(false);
            MessageBox.Show(ex.Message, "同步失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void SyncNow()
    {
        Reload();
        if (IsOnline)
        {
            MessageBox.Show("已同步云端数据。", "同步完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void ToggleDashboardCard(DashboardCard? card)
    {
        if (card is null)
        {
            return;
        }

        ExpandedDashboardCard = ReferenceEquals(ExpandedDashboardCard, card) || ExpandedDashboardCard?.Key == card.Key
            ? null
            : card;
    }

    [RelayCommand]
    private void OpenDashboardDetail(DashboardDetailItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.TargetType == "Employee")
        {
            NavigateToEmployee(item.TargetId, item.DepartmentName);
            return;
        }

        if (item.TargetType == "Task")
        {
            NavigateToEmployee(item.TargetId, item.DepartmentName);
            return;
        }

        if (item.TargetType == "Project")
        {
            SelectedNavigation = NavigationItems.FirstOrDefault(x => x.Title == "项目中心");
            ProjectSearchKeyword = string.Empty;
            SelectedProjectSummary = ProjectCenterItems.FirstOrDefault(project => project.ProjectId == item.TargetId)
                ?? BuildProjectSummaries(_repository.GetSnapshot()).FirstOrDefault(project => project.ProjectId == item.TargetId);
        }
    }

    private void NavigateToEmployee(int employeeId, string departmentName)
    {
        SelectedNavigation = NavigationItems.FirstOrDefault(x => x.Title == "人员管理");
        SelectedDepartmentPage = DepartmentPages.FirstOrDefault(page => page.DepartmentName == departmentName)
            ?? DepartmentPages.Select(page => new
                {
                    Page = page,
                    Employee = page.EmployeeGroups.FirstOrDefault(group => group.EmployeeId == employeeId)
                })
                .FirstOrDefault(x => x.Employee is not null)
                ?.Page
            ?? SelectedDepartmentPage;
    }

    [RelayCommand]
    private async Task CheckUpdate()
    {
        if (!_updateService.CanCheckOnline)
        {
            MessageBox.Show("当前是本机模式，连接云端后可以检查在线更新。", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            UpdateStatusText = "正在检查更新";
            var updateInfo = await _updateService.CheckLatestAsync();
            if (updateInfo is null)
            {
                UpdateStatusText = "未获取到版本信息";
                MessageBox.Show("暂时没有获取到云端版本信息。", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!updateInfo.HasUpdate)
            {
                UpdateStatusText = $"已是最新版本 {updateInfo.CurrentVersion}";
                MessageBox.Show($"当前已是最新版本：{updateInfo.CurrentVersion}", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            UpdateStatusText = $"发现新版本 {updateInfo.LatestVersion}";
            var message = string.IsNullOrWhiteSpace(updateInfo.Notes)
                ? $"发现新版本 {updateInfo.LatestVersion}，当前版本 {updateInfo.CurrentVersion}。是否打开下载页面？"
                : $"发现新版本 {updateInfo.LatestVersion}，当前版本 {updateInfo.CurrentVersion}。\n\n更新内容：{updateInfo.Notes}\n\n是否打开下载页面？";
            var result = MessageBox.Show(message, "发现新版本", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                _updateService.OpenDownload(updateInfo);
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText = "更新检查失败";
            MessageBox.Show(ex.Message, "检查更新失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void AddEmployee(DepartmentWorkPage? departmentPage)
    {
        if (departmentPage is null || string.IsNullOrWhiteSpace(departmentPage.NewEmployeeName))
        {
            return;
        }

        _repository.AddEmployee(departmentPage.NewEmployeeName.Trim(), string.Empty, departmentPage.DepartmentName);
        departmentPage.NewEmployeeName = string.Empty;
        NewEmployeeName = string.Empty;
        NewEmployeeRole = string.Empty;
        HasPendingChanges = true;
        Reload();
    }

    [RelayCommand]
    private void RemoveEmployee(EmployeeWorkGroup? employeeGroup)
    {
        if (employeeGroup is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"确定删除“{employeeGroup.EmployeeName}”吗？该人员下面的工作安排也会一起移除。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _repository.DeleteEmployee(employeeGroup.EmployeeId);
        Reload();
    }

    [RelayCommand]
    private void AddProjectRow(EmployeeWorkGroup? employeeGroup)
    {
        if (employeeGroup is null)
        {
            return;
        }

        var firstProject = ProjectOptions.FirstOrDefault();
        employeeGroup.ProjectRows.Add(new EmployeeProjectRow
        {
            ProjectId = firstProject?.ProjectId ?? 0,
            ProjectName = firstProject?.Name ?? "新项目",
            ProjectCode = firstProject?.ProjectCode ?? string.Empty,
            CurrentTask = "请输入当前工作",
            SubmissionDate = DateTime.Today,
            TagColor = firstProject?.TagColor ?? "#DCEBFF",
            SelectedProject = firstProject
        });
        HasPendingChanges = true;
    }

    [RelayCommand]
    private void RemoveProjectRow(EmployeeProjectRow? row)
    {
        if (row is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"确定删除“{row.CurrentTask}”吗？删除后这条工作安排会从当前列表移除。",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var group = DepartmentPages
            .SelectMany(page => page.EmployeeGroups)
            .FirstOrDefault(g => g.ProjectRows.Contains(row));

        group?.ProjectRows.Remove(row);
        HasPendingChanges = true;
    }

    [RelayCommand]
    private void CompleteProjectRow(EmployeeProjectRow? row)
    {
        if (row is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"确定完成“{row.CurrentTask}”吗？确认后这条工作安排会从当前列表移除。",
            "确认完成",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var group = DepartmentPages
            .SelectMany(page => page.EmployeeGroups)
            .FirstOrDefault(g => g.ProjectRows.Contains(row));
        if (row.TaskId is not null)
        {
            _repository.CompleteTaskWithFollowUp(row.TaskId.Value);
            Reload();
            return;
        }

        if (row.ProjectId > 0)
        {
            _repository.AddProjectFollowUp(row.ProjectId, null, row.CurrentTask.Trim(), group?.EmployeeName ?? string.Empty, DateTime.Now);
            HasPendingChanges = false;
            Reload();
            return;
        }

        if (row.TaskId is not null)
        {
            _repository.DeleteTask(row.TaskId.Value);
            return;
        }

        group?.ProjectRows.Remove(row);
        HasPendingChanges = true;
    }

    [RelayCommand]
    private void ExportDepartment(DepartmentWorkPage? departmentPage)
    {
        TryExport(() =>
        {
            var department = string.IsNullOrWhiteSpace(departmentPage?.DepartmentName) ? "人员管理" : departmentPage.DepartmentName.Trim();
            return PdfExportService.ExportDepartmentSchedule(department, departmentPage?.EmployeeGroups ?? []);
        });
    }

    [RelayCommand]
    private void ExportActiveProjects()
    {
        TryExport(() =>
        {
            var snapshot = _repository.GetSnapshot();
            var summaries = BuildProjectSummaries(snapshot)
                .Where(project => IsActiveProject(project, snapshot))
                .OrderBy(project => project.EndDate)
                .ToList();

            return PdfExportService.ExportProjectList("当前正在进行的项目", summaries, "项目导出", "进行中项目");
        });
    }

    [RelayCommand]
    private void ExportAllProjects()
    {
        TryExport(() =>
        {
            var summaries = BuildProjectSummaries(_repository.GetSnapshot())
                .OrderByDescending(project => project.StartDate)
                .ToList();

            return PdfExportService.ExportProjectList("公司所有项目", summaries, "项目导出", "公司所有项目");
        });
    }

    [RelayCommand]
    private void CreateProject()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName) || SelectedManager is null)
        {
            return;
        }

        var startDate = NewProjectStartDate.Date;
        var endDate = startDate.AddDays(45);
        NewProjectEndDate = endDate;

        _repository.CreateProject(
            NewProjectName.Trim(),
            NewProjectArea.Trim(),
            NewProjectType.Trim(),
            SelectedManager.Id,
            startDate,
            endDate,
            NewProjectSummary.Trim());
        NewProjectName = string.Empty;
        NewProjectArea = string.Empty;
        NewProjectType = string.Empty;
        NewProjectSummary = "新项目待补充设计任务、交付节点和资料要求。";
        HasPendingChanges = false;
    }

    [RelayCommand]
    private void SaveProjectDetail()
    {
        if (SelectedProjectSummary is null || string.IsNullOrWhiteSpace(EditableProjectName))
        {
            return;
        }

        _repository.UpdateProject(
            SelectedProjectSummary.ProjectId,
            EditableProjectName.Trim(),
            EditableProjectArea.Trim(),
            EditableProjectType.Trim(),
            EditableProjectOperators.Trim(),
            EditableProjectSummary.Trim(),
            EditableTaskPlanText.Trim());

        HasPendingChanges = false;
        Reload();
        MessageBox.Show("项目中心内容已保存到数据库。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SaveAllPendingChanges()
    {
        var project = SelectedProjectSummary;
        var projectName = EditableProjectName.Trim();
        var projectArea = EditableProjectArea.Trim();
        var projectType = EditableProjectType.Trim();
        var projectOperators = EditableProjectOperators.Trim();
        var projectSummary = EditableProjectSummary.Trim();
        var projectTaskPlan = EditableTaskPlanText.Trim();
        var personnelRows = DepartmentPages
            .SelectMany(page => page.EmployeeGroups)
            .SelectMany(group => group.ProjectRows.Select(row => new
            {
                group.EmployeeId,
                row.TaskId,
                row.ProjectId,
                TaskName = row.CurrentTask.Trim(),
                row.SubmissionDate
            }))
            .Where(row => row.ProjectId > 0 && !string.IsNullOrWhiteSpace(row.TaskName))
            .ToList();

        if (project is not null && !string.IsNullOrWhiteSpace(projectName))
        {
            _repository.UpdateProject(project.ProjectId, projectName, projectArea, projectType, projectOperators, projectSummary, projectTaskPlan);
        }

        foreach (var row in personnelRows)
        {
            if (row.TaskId is null)
            {
                _repository.AddTask(row.ProjectId, row.TaskName, row.EmployeeId, 30, 0, DateTime.Today, row.SubmissionDate, "进行中");
                continue;
            }

            _repository.UpdateTaskFromSchedule(row.TaskId.Value, row.ProjectId, row.TaskName, row.SubmissionDate);
        }

        HasPendingChanges = false;
        Reload();
    }

    [RelayCommand]
    private void SavePersonnelRows()
    {
        var personnelRows = DepartmentPages
            .SelectMany(page => page.EmployeeGroups)
            .SelectMany(group => group.ProjectRows.Select(row => new
            {
                group.EmployeeId,
                row.TaskId,
                row.ProjectId,
                TaskName = row.CurrentTask.Trim(),
                row.SubmissionDate
            }))
            .Where(row => row.ProjectId > 0 && !string.IsNullOrWhiteSpace(row.TaskName))
            .ToList();

        foreach (var row in personnelRows)
        {
            if (row.TaskId is null)
            {
                _repository.AddTask(row.ProjectId, row.TaskName, row.EmployeeId, 30, 0, DateTime.Today, row.SubmissionDate, "进行中");
                continue;
            }

            _repository.UpdateTaskFromSchedule(row.TaskId.Value, row.ProjectId, row.TaskName, row.SubmissionDate);
        }

        HasPendingChanges = false;
        Reload();
        MessageBox.Show("人员管理内容已保存到数据库。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void DeleteSelectedProject()
    {
        if (SelectedProjectSummary is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"确定删除项目“{SelectedProjectSummary.Name}”吗？\n删除后项目详情、跟进记录、任务和资料都会一起移除，且无法恢复。",
            "删除项目确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _repository.DeleteProject(SelectedProjectSummary.ProjectId);
    }

    [RelayCommand]
    private void DeleteProjectFollowUp(ProjectFollowUp? followUp)
    {
        if (followUp is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"确定删除这条完成进度吗？\n{followUp.Content}",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _repository.DeleteProjectFollowUp(followUp.Id);
        MessageBox.Show("完成进度已删除。", "删除成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void AddTask()
    {
        if (SelectedProjectSummary is null || SelectedTaskOwner is null || string.IsNullOrWhiteSpace(NewTaskName))
        {
            return;
        }

        _repository.AddTask(SelectedProjectSummary.ProjectId, NewTaskName.Trim(), SelectedTaskOwner.Id, 30, NewTaskProgress, NewTaskStartDate, NewTaskEndDate, SelectedTaskStatus);
        NewTaskName = string.Empty;
        NewTaskProgress = 0;
        NewTaskStartDate = DateTime.Today;
        NewTaskEndDate = DateTime.Today.AddDays(5);
        Reload();
    }

    [RelayCommand]
    private void AddSiteVisit()
    {
        if (SelectedProjectSummary is null || string.IsNullOrWhiteSpace(NewVisitIssues))
        {
            return;
        }

        _repository.AddSiteVisit(SelectedProjectSummary.ProjectId, NewVisitDate, NewVisitIssues.Trim(), NewVisitSuggestions.Trim(), string.Empty, SelectedVisitStatus);
        NewVisitIssues = string.Empty;
        NewVisitSuggestions = string.Empty;
        Reload();
    }

    [RelayCommand]
    private void AddAcceptance()
    {
        if (SelectedProjectSummary is null || string.IsNullOrWhiteSpace(NewAcceptanceResult))
        {
            return;
        }

        _repository.AddAcceptanceRecord(SelectedProjectSummary.ProjectId, NewAcceptanceDate, NewAcceptanceResult.Trim(), NewAcceptanceRectificationItems.Trim(), NewAcceptanceReviewRecord.Trim(), SelectedAcceptanceStatus);
        NewAcceptanceResult = string.Empty;
        NewAcceptanceRectificationItems = string.Empty;
        NewAcceptanceReviewRecord = string.Empty;
        Reload();
    }

    [RelayCommand]
    private void AddFileRecord()
    {
        if (SelectedProjectSummary is null || string.IsNullOrWhiteSpace(NewFileName))
        {
            return;
        }

        var virtualPath = $"{SelectedProjectSummary.ProjectCode}\\{SelectedFileCategory}\\{NewFileName.Trim()}";
        _repository.AddProjectFile(SelectedProjectSummary.ProjectId, SelectedFileCategory, NewFileName.Trim(), virtualPath);
        NewFileName = string.Empty;
        NewFilePath = string.Empty;
        Reload();
    }

    [RelayCommand]
    private void ToggleStage(ProjectStage? stage)
    {
        if (stage is null)
        {
            return;
        }

        _repository.ToggleStageCompletion(stage.Id, stage.CompletedDate is null);
        Reload();
    }

    private void RefreshCollections(WorkspaceSnapshot snapshot)
    {
        Managers.Reset(snapshot.Employees);
        Employees.Reset(snapshot.Employees);
        SelectedManager ??= Managers.FirstOrDefault();
        SelectedTaskOwner ??= Employees.FirstOrDefault();

        var summaries = BuildProjectSummaries(snapshot);
        var projectOptions = BuildProjectOptions(snapshot);
        var previousProjectId = SelectedProjectSummary?.ProjectId;
        var previousDepartmentName = SelectedDepartmentPage?.DepartmentName;

        DashboardCards.Reset(BuildDashboardCards(snapshot, summaries));
        RecentUpdates.Reset(summaries.OrderByDescending(x => x.EndDate).Take(4).Select(summary =>
        {
            var project = snapshot.Projects.First(p => p.Id == summary.ProjectId);
            return new RecentProjectUpdate
            {
                ProjectName = summary.Name,
                Status = summary.Status,
                Summary = project.Summary,
                UpdatedAt = project.UpdatedAt
            };
        }));

        ProjectOptions.Reset(projectOptions);
        var spaceGroups = BuildEmployeeGroups(snapshot, projectOptions, DepartmentNames[0]);
        EmployeeGroups.Reset(spaceGroups);
        DepartmentPages.Reset(BuildDepartmentPages(snapshot, projectOptions));
        AttachPendingChangeTracking();
        SelectedDepartmentPage = DepartmentPages.FirstOrDefault(page => page.DepartmentName == previousDepartmentName)
            ?? DepartmentPages.FirstOrDefault();
        ProjectCenterItems.Reset(summaries.OrderBy(x => x.EndDate));
        ProjectTreeGroups.Reset(BuildProjectTree(summaries));
        AllTimelineItems.Reset(BuildTimeline(snapshot).OrderBy(t => t.PlannedDate));

        SelectedProjectSummary = summaries.FirstOrDefault(x => x.ProjectId == previousProjectId) ?? summaries.FirstOrDefault();
        LoadProjectDetail(snapshot);
    }

    private void LoadProjectDetail()
    {
        LoadProjectDetail(_repository.GetSnapshot());
    }

    private void LoadProjectDetail(WorkspaceSnapshot snapshot)
    {
        if (SelectedProjectSummary is null)
        {
            EditableProjectName = string.Empty;
            EditableProjectArea = string.Empty;
            EditableProjectType = string.Empty;
            EditableProjectOperators = string.Empty;
            EditableProjectSummary = string.Empty;
            EditableTaskPlanText = string.Empty;
            SelectedProjectStages.Clear();
            SelectedProjectTasks.Clear();
            SelectedSiteVisits.Clear();
            SelectedAcceptanceRecords.Clear();
            SelectedProjectFiles.Clear();
            SelectedProjectFollowUps.Clear();
            HasPendingChanges = false;
            return;
        }

        EditableProjectName = SelectedProjectSummary.Name;
        EditableProjectArea = SelectedProjectSummary.Area;
        EditableProjectType = SelectedProjectSummary.ProjectType;
        EditableProjectOperators = SelectedProjectSummary.OperatorNames;
        var selectedProject = snapshot.Projects.FirstOrDefault(x => x.Id == SelectedProjectSummary.ProjectId);
        EditableProjectSummary = selectedProject?.Summary ?? string.Empty;
        EditableTaskPlanText = selectedProject?.TaskPlan ?? string.Empty;

        SelectedProjectStages.Reset(snapshot.ProjectStages.Where(x => x.ProjectId == SelectedProjectSummary.ProjectId).OrderBy(x => x.SequenceNo));
        SelectedProjectTasks.Reset(snapshot.Tasks.Where(x => x.ProjectId == SelectedProjectSummary.ProjectId).OrderBy(x => x.EndDate).Select(task => new ProjectTaskDisplayItem
        {
            TaskId = task.Id,
            TaskName = task.Name,
            OwnerName = snapshot.Employees.FirstOrDefault(x => x.Id == task.OwnerId)?.Name ?? "未分配",
            Status = task.Status,
            ProgressPercent = task.ProgressPercent,
            EndDate = task.EndDate
        }));
        SelectedSiteVisits.Reset(snapshot.SiteVisits.Where(x => x.ProjectId == SelectedProjectSummary.ProjectId).OrderByDescending(x => x.VisitDate));
        SelectedAcceptanceRecords.Reset(snapshot.AcceptanceRecords.Where(x => x.ProjectId == SelectedProjectSummary.ProjectId).OrderByDescending(x => x.AcceptanceDate));
        SelectedProjectFiles.Reset(snapshot.ProjectFiles.Where(x => x.ProjectId == SelectedProjectSummary.ProjectId).OrderByDescending(x => x.UploadedAt));
        SelectedProjectFollowUps.Reset(snapshot.ProjectFollowUps.Where(x => x.ProjectId == SelectedProjectSummary.ProjectId).OrderByDescending(x => x.CompletedAt));
        HasPendingChanges = false;
    }

    private void AttachPendingChangeTracking()
    {
        foreach (var page in DepartmentPages)
        {
            page.PropertyChanged -= DepartmentPageOnPropertyChanged;
            page.PropertyChanged += DepartmentPageOnPropertyChanged;

            foreach (var group in page.EmployeeGroups)
            {
                group.PropertyChanged -= EmployeeWorkGroupOnPropertyChanged;
                group.PropertyChanged += EmployeeWorkGroupOnPropertyChanged;

                foreach (var row in group.ProjectRows)
                {
                    row.PropertyChanged -= EmployeeProjectRowOnPropertyChanged;
                    row.PropertyChanged += EmployeeProjectRowOnPropertyChanged;
                }
            }
        }
    }

    private void DepartmentPageOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DepartmentWorkPage.NewEmployeeName))
        {
            HasPendingChanges = true;
        }
    }

    private void EmployeeWorkGroupOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EmployeeWorkGroup.ProjectRows))
        {
            HasPendingChanges = true;
        }
    }

    private void EmployeeProjectRowOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EmployeeProjectRow.CurrentTask)
            or nameof(EmployeeProjectRow.SubmissionDate)
            or nameof(EmployeeProjectRow.SelectedProject))
        {
            HasPendingChanges = true;
        }
    }

    partial void OnEditableProjectNameChanged(string value) => HasPendingChanges = true;
    partial void OnEditableProjectAreaChanged(string value) => HasPendingChanges = true;
    partial void OnEditableProjectTypeChanged(string value) => HasPendingChanges = true;
    partial void OnEditableProjectOperatorsChanged(string value) => HasPendingChanges = true;
    partial void OnEditableProjectSummaryChanged(string value) => HasPendingChanges = true;
    partial void OnEditableTaskPlanTextChanged(string value) => HasPendingChanges = true;

    partial void OnIsOnlineChanged(bool value) => OnPropertyChanged(nameof(ConnectionIndicatorBrush));

    private void UpdateConnectionStatus(bool requestSucceeded)
    {
        var online = !_repository.IsCloudMode || (requestSucceeded && _repository.TestConnection());
        IsOnline = online;
        ConnectionStatusText = _repository.IsCloudMode
            ? online ? "云端在线" : "云端离线"
            : "本机模式";
        LastSyncText = online ? $"上次同步 {DateTime.Now:HH:mm:ss}" : "同步失败";
    }

    public bool ConfirmCloseAndSave()
    {
        if (!HasPendingChanges)
        {
            return true;
        }

        var result = MessageBox.Show(
            "当前内容还没有保存。选择“是”立即保存，选择“否”直接关闭，选择“取消”返回继续编辑。",
            "关闭前提示",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.Yes)
        {
            SaveAllPendingChanges();
        }

        return true;
    }

    private void ApplyProjectFilters()
    {
        var snapshot = _repository.GetSnapshot();
        var summaries = BuildProjectSummaries(snapshot);
        IEnumerable<ProjectSummary> query = summaries;

        if (!string.Equals(SelectedProjectStatus, "全部状态", StringComparison.Ordinal))
        {
            query = query.Where(x => x.Status == SelectedProjectStatus);
        }

        if (!string.IsNullOrWhiteSpace(ProjectSearchKeyword))
        {
            query = query.Where(x =>
                x.Name.Contains(ProjectSearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                x.ManagerName.Contains(ProjectSearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                x.CurrentStage.Contains(ProjectSearchKeyword, StringComparison.OrdinalIgnoreCase));
        }

        var filteredProjects = query.ToList();
        ProjectCenterItems.Reset(filteredProjects.OrderBy(x => x.EndDate));
        ProjectTreeGroups.Reset(BuildProjectTree(filteredProjects));

        if (!string.IsNullOrWhiteSpace(ProjectSearchKeyword))
        {
            var currentStillVisible = SelectedProjectSummary is not null
                && filteredProjects.Any(project => project.ProjectId == SelectedProjectSummary.ProjectId);
            if (!currentStillVisible)
            {
                SelectedProjectSummary = filteredProjects.FirstOrDefault();
            }
        }
    }

    private static void TryExport(Func<string> exportAction)
    {
        try
        {
            var filePath = exportAction();
            MessageBox.Show($"已导出到：\n{filePath}", "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"导出失败：{ex.Message}", "导出失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static List<ProjectSummary> BuildProjectSummaries(WorkspaceSnapshot snapshot)
    {
        return snapshot.Projects.Select(project =>
        {
            var stages = snapshot.ProjectStages.Where(x => x.ProjectId == project.Id).OrderBy(x => x.SequenceNo).ToList();
            var completed = stages.Count(x => x.CompletedDate is not null);
            var progress = stages.Count == 0 ? 0 : (int)Math.Round((double)completed / stages.Count * 100);
            var currentStage = stages.FirstOrDefault(x => x.CompletedDate is null)?.StageName ?? "项目归档";
            var isDelayed = stages.Any(x => x.CompletedDate is null && x.PlannedDate.Date < DateTime.Today);
            var managerName = snapshot.Employees.FirstOrDefault(e => e.Id == project.ManagerId)?.Name ?? "未分配";
            var taskOperators = snapshot.Tasks
                .Where(task => task.ProjectId == project.Id)
                .Select(task => snapshot.Employees.FirstOrDefault(employee => employee.Id == task.OwnerId)?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();

            return new ProjectSummary
            {
                ProjectId = project.Id,
                ProjectCode = project.ProjectCode,
                Name = project.Name,
                Status = project.Status,
                ManagerName = managerName,
                Area = string.IsNullOrWhiteSpace(project.Area) ? "待填写" : project.Area,
                ProjectType = string.IsNullOrWhiteSpace(project.ProjectType) ? "待填写" : project.ProjectType,
                OperatorNames = string.IsNullOrWhiteSpace(project.OperatorNames)
                    ? taskOperators.Count == 0 ? managerName : string.Join("、", taskOperators)
                    : project.OperatorNames,
                CurrentStage = currentStage,
                ProgressPercent = progress,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                IsDelayed = isDelayed,
                TaskCount = snapshot.Tasks.Count(t => t.ProjectId == project.Id),
                Summary = project.Summary
            };
        }).ToList();
    }

    private static IEnumerable<ProjectYearGroup> BuildProjectTree(IEnumerable<ProjectSummary> summaries)
    {
        return summaries
            .GroupBy(project => project.StartDate.Year)
            .OrderByDescending(group => group.Key)
            .Select(yearGroup => new ProjectYearGroup
            {
                Year = yearGroup.Key,
                Months = new ObservableCollection<ProjectMonthGroup>(
                    yearGroup
                        .GroupBy(project => project.StartDate.Month)
                        .OrderByDescending(group => group.Key)
                        .Select(monthGroup => new ProjectMonthGroup
                        {
                            Month = monthGroup.Key,
                            Projects = new ObservableCollection<ProjectSummary>(monthGroup.OrderBy(project => project.Name))
                        }))
            });
    }

    private static IEnumerable<DashboardCard> BuildDashboardCards(WorkspaceSnapshot snapshot, List<ProjectSummary> summaries)
    {
        var activeProjects = summaries
            .Where(project => HasOpenTask(project, snapshot))
            .OrderBy(project => project.EndDate)
            .ToList();
        var dueTasks = snapshot.Tasks
            .Where(task =>
                !string.Equals(task.Status, "已完成", StringComparison.Ordinal)
                && task.EndDate.Date >= DateTime.Today
                && task.EndDate.Date <= DateTime.Today.AddDays(7))
            .OrderBy(task => task.EndDate)
            .ToList();
        var overdueTasks = snapshot.Tasks
            .Where(task =>
                !string.Equals(task.Status, "已完成", StringComparison.Ordinal)
                && task.EndDate.Date < DateTime.Today)
            .OrderBy(task => task.EndDate)
            .ToList();

        return
        [
            new DashboardCard
            {
                Key = "Employees",
                Title = "公司人员",
                Value = snapshot.Employees.Count.ToString(),
                Subtitle = "按部门查看全部人员",
                Groups = BuildEmployeeDashboardGroups(snapshot)
            },
            new DashboardCard
            {
                Key = "ActiveProjects",
                Title = "进行中项目",
                Value = activeProjects.Count.ToString(),
                Subtitle = "按项目查看全部人员和对应任务",
                Groups = BuildActiveProjectDashboardGroups(activeProjects, snapshot)
            },
            new DashboardCard
            {
                Key = "DueTasks",
                Title = "本周节点",
                Value = dueTasks.Count.ToString(),
                Subtitle = "未来7天内需要提交的内容",
                Groups = BuildProjectTaskDashboardGroups(dueTasks, snapshot)
            },
            new DashboardCard
            {
                Key = "OverdueTasks",
                Title = "逾期任务",
                Value = overdueTasks.Count.ToString(),
                Subtitle = "已超过提交日期的项目、人员和工作",
                Groups = BuildProjectTaskDashboardGroups(overdueTasks, snapshot)
            }
        ];
    }

    private static List<DashboardDetailGroup> BuildEmployeeDashboardGroups(WorkspaceSnapshot snapshot)
    {
        return DepartmentNames
            .Select(department => new DashboardDetailGroup
            {
                Title = department,
                Items = snapshot.Employees
                    .Where(employee => IsEmployeeInDepartment(employee, department))
                    .OrderBy(employee => employee.Name)
                    .Select(employee => new DashboardDetailItem
                    {
                        Title = employee.Name,
                        Subtitle = string.IsNullOrWhiteSpace(employee.Role) ? "点击查看人员安排" : employee.Role,
                        TargetType = "Employee",
                        TargetId = employee.Id,
                        DepartmentName = department
                    })
                    .ToList()
            })
            .Where(group => group.Items.Count > 0)
            .ToList();
    }

    private static List<DashboardDetailGroup> BuildProjectDashboardGroups(string title, IEnumerable<ProjectSummary> projects)
    {
        return
        [
            new DashboardDetailGroup
            {
                Title = title,
                Items = projects.Select(project => new DashboardDetailItem
                {
                    Title = project.Name,
                    Subtitle = $"{project.CurrentStage} · {project.EndDate:yyyy-MM-dd}",
                    TargetType = "Project",
                    TargetId = project.ProjectId
                }).ToList()
            }
        ];
    }

    private static List<DashboardDetailGroup> BuildActiveProjectDashboardGroups(IEnumerable<ProjectSummary> projects, WorkspaceSnapshot snapshot)
    {
        return projects
            .Select(project =>
            {
                var tasks = snapshot.Tasks
                    .Where(task => task.ProjectId == project.ProjectId && !string.Equals(task.Status, "已完成", StringComparison.Ordinal))
                    .OrderBy(task => task.EndDate)
                    .ToList();

                return new DashboardDetailGroup
                {
                    Title = project.Name,
                    Items = tasks.Count == 0
                        ? [new DashboardDetailItem
                        {
                            Title = string.Empty,
                            Subtitle = string.Empty,
                            TargetType = "Project",
                            TargetId = project.ProjectId
                        }]
                        : tasks.Select(task =>
                        {
                            var owner = snapshot.Employees.FirstOrDefault(employee => employee.Id == task.OwnerId);
                            return new DashboardDetailItem
                            {
                                Title = owner?.Name ?? string.Empty,
                                Subtitle = task.Name,
                                TargetType = owner is null ? "Project" : "Task",
                                TargetId = owner?.Id ?? project.ProjectId,
                                DepartmentName = ResolveEmployeeDepartment(owner)
                            };
                        }).ToList()
                };
            })
            .OrderBy(group => group.Title)
            .ToList();
    }

    private static List<DashboardDetailGroup> BuildProjectTaskDashboardGroups(IEnumerable<WorkTask> tasks, WorkspaceSnapshot snapshot)
    {
        return tasks
            .GroupBy(task => task.ProjectId)
            .Select(group =>
            {
                var project = snapshot.Projects.FirstOrDefault(x => x.Id == group.Key);
                return new DashboardDetailGroup
                {
                    Title = project?.Name ?? "未知项目",
                    Items = group
                        .OrderBy(task => task.EndDate)
                        .Select(task =>
                        {
                            var owner = snapshot.Employees.FirstOrDefault(x => x.Id == task.OwnerId);
                            var department = ResolveEmployeeDepartment(owner);
                            return new DashboardDetailItem
                            {
                                Title = owner is null ? "未分配人员" : owner.Name,
                                Subtitle = $"{task.Name} · 提交 {task.EndDate:yyyy-MM-dd}",
                                TargetType = "Task",
                                TargetId = owner?.Id ?? 0,
                                DepartmentName = department
                            };
                        })
                        .ToList()
                };
            })
            .OrderBy(group => group.Title)
            .ToList();
    }

    private static string ResolveEmployeeDepartment(Employee? employee)
    {
        if (employee is null)
        {
            return DepartmentNames[0];
        }

        return DepartmentNames.Contains(employee.Department) ? employee.Department : DepartmentNames[0];
    }

    private static bool IsActiveProject(ProjectSummary project, WorkspaceSnapshot snapshot)
    {
        if (string.Equals(project.Status, "已归档", StringComparison.Ordinal))
        {
            return false;
        }

        var hasOpenTask = snapshot.Tasks.Any(task =>
            task.ProjectId == project.ProjectId
            && !string.Equals(task.Status, "已完成", StringComparison.Ordinal));
        var hasOpenStage = snapshot.ProjectStages.Any(stage =>
            stage.ProjectId == project.ProjectId
            && stage.CompletedDate is null);

        return hasOpenTask || hasOpenStage || project.EndDate.Date >= DateTime.Today;
    }

    private static bool HasOpenTask(ProjectSummary project, WorkspaceSnapshot snapshot) =>
        snapshot.Tasks.Any(task =>
            task.ProjectId == project.ProjectId
            && !string.Equals(task.Status, "已完成", StringComparison.Ordinal));

    private static List<ProjectOption> BuildProjectOptions(WorkspaceSnapshot snapshot)
    {
        var palette = new[]
        {
            "#DCEBFF",
            "#E6F4E8",
            "#FCE8D7",
            "#F3E5FF",
            "#FFE7EA",
            "#E6F7F5"
        };

        return snapshot.Projects
            .OrderBy(project => project.ProjectCode)
            .Select((project, index) => new ProjectOption
            {
                ProjectId = project.Id,
                ProjectCode = project.ProjectCode,
                Name = project.Name,
                TagColor = palette[index % palette.Length]
            })
            .ToList();
    }

    private static List<DepartmentWorkPage> BuildDepartmentPages(WorkspaceSnapshot snapshot, IReadOnlyCollection<ProjectOption> projectOptions)
    {
        return DepartmentNames.Select(department => new DepartmentWorkPage
        {
            DepartmentName = department,
            EmployeeGroups = new ObservableCollection<EmployeeWorkGroup>(BuildEmployeeGroups(snapshot, projectOptions, department))
        }).ToList();
    }

    private static List<EmployeeWorkGroup> BuildEmployeeGroups(WorkspaceSnapshot snapshot, IReadOnlyCollection<ProjectOption> projectOptions, string departmentName)
    {
        return snapshot.Employees
            .Where(employee => IsEmployeeInDepartment(employee, departmentName))
            .Select(employee =>
            {
                var rows = snapshot.Tasks
                    .Where(task => task.OwnerId == employee.Id && task.Status != "已完成")
                    .OrderBy(task => task.EndDate)
                    .Select(task =>
                    {
                        var project = snapshot.Projects.FirstOrDefault(x => x.Id == task.ProjectId);
                        var option = projectOptions.FirstOrDefault(x => x.ProjectId == task.ProjectId);
                        return new EmployeeProjectRow
                        {
                            ProjectId = project?.Id ?? 0,
                            ProjectName = project?.Name ?? "未知项目",
                            ProjectCode = project?.ProjectCode ?? string.Empty,
                            TaskId = task.Id,
                            CurrentTask = task.Name,
                            SubmissionDate = task.EndDate,
                            TagColor = option?.TagColor ?? "#E5E5EA",
                            SelectedProject = option
                        };
                    });

                return new EmployeeWorkGroup
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.Name,
                    Role = employee.Role,
                    ProjectRows = new ObservableCollection<EmployeeProjectRow>(rows)
                };
            }).ToList();
    }

    private static bool IsEmployeeInDepartment(Employee employee, string departmentName)
    {
        if (string.Equals(employee.Department, departmentName, StringComparison.Ordinal))
        {
            return true;
        }

        return departmentName == DepartmentNames[0] && !DepartmentNames.Contains(employee.Department);
    }

    private static List<TimelineItem> BuildTimeline(WorkspaceSnapshot snapshot)
    {
        return snapshot.ProjectStages.Select(stage => new TimelineItem
        {
            ProjectName = snapshot.Projects.FirstOrDefault(p => p.Id == stage.ProjectId)?.Name ?? string.Empty,
            StageName = stage.StageName,
            PlannedDate = stage.PlannedDate,
            Status = stage.Status,
            IsDelayed = stage.CompletedDate is null && stage.PlannedDate.Date < DateTime.Today
        }).ToList();
    }

    private string BuildDepartmentExcelHtml(string departmentName, DepartmentWorkPage? departmentPage)
    {
        var groups = departmentPage?.EmployeeGroups ?? [];
        var builder = new StringBuilder();
        builder.AppendLine("""
            <html>
            <head>
            <meta charset="utf-8">
            <style>
            table { border-collapse: collapse; font-family: "Microsoft YaHei", Arial, sans-serif; font-size: 12pt; }
            th { background: #f0f1f4; font-weight: 700; }
            th, td { border: 1px solid #d9d9df; padding: 8px 12px; mso-number-format:"\@"; }
            .title { font-size: 18pt; font-weight: 700; padding: 12px 0; }
            </style>
            </head>
            <body>
            """);
        builder.AppendLine($"<div class=\"title\">{Encode(departmentName)}人员排期</div>");
        builder.AppendLine("<table>");
        builder.AppendLine("<tr><th>部门</th><th>姓名</th><th>项目</th><th>当前工作</th><th>提交日期</th><th>距离提交</th></tr>");

        foreach (var employee in groups)
        {
            if (employee.ProjectRows.Count == 0)
            {
                builder.AppendLine($"<tr><td>{Encode(departmentName)}</td><td>{Encode(employee.EmployeeName)}</td><td></td><td></td><td></td><td></td></tr>");
                continue;
            }

            foreach (var row in employee.ProjectRows)
            {
                builder.AppendLine("<tr>");
                builder.AppendLine($"<td>{Encode(departmentName)}</td>");
                builder.AppendLine($"<td>{Encode(employee.EmployeeName)}</td>");
                builder.AppendLine($"<td>{Encode(row.ProjectName)}</td>");
                builder.AppendLine($"<td>{Encode(row.CurrentTask)}</td>");
                builder.AppendLine($"<td>{row.SubmissionDate:yyyy-MM-dd}</td>");
                builder.AppendLine($"<td>{Encode(row.DaysUntilSubmissionText)}</td>");
                builder.AppendLine("</tr>");
            }
        }

        builder.AppendLine("</table></body></html>");
        return builder.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private bool IsMenu(string title) => SelectedNavigation?.Title == title;
}

internal static class CollectionExtensions
{
    public static void Reset<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
