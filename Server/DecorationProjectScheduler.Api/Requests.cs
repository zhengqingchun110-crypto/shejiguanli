namespace DecorationProjectScheduler.Api;

public sealed record AddEmployeeRequest(string Name, string Role, string Department);

public sealed record CreateProjectRequest(
    string Name,
    string Area,
    string ProjectType,
    int ManagerId,
    DateTime StartDate,
    DateTime EndDate,
    string Summary);

public sealed record UpdateProjectRequest(
    string Name,
    string Area,
    string ProjectType,
    string OperatorNames,
    string Summary,
    string TaskPlan);

public sealed record AddTaskRequest(
    int ProjectId,
    string Name,
    int OwnerId,
    int WorkloadPercent,
    int ProgressPercent,
    DateTime StartDate,
    DateTime EndDate,
    string Status);

public sealed record UpdateTaskRequest(string Name);

public sealed record UpdateTaskScheduleRequest(int ProjectId, string Name, DateTime EndDate);

public sealed record AddProjectFollowUpRequest(int ProjectId, int? TaskId, string Content, string OperatorName, DateTime CompletedAt);

public sealed record AddSiteVisitRequest(int ProjectId, DateTime VisitDate, string Issues, string Suggestions, string PhotoPath, string RectificationStatus);

public sealed record AddAcceptanceRequest(int ProjectId, DateTime AcceptanceDate, string Result, string RectificationItems, string ReviewRecord, string Status);

public sealed record AddProjectFileRequest(int ProjectId, string Category, string FileName, string FilePath, long FileSizeBytes = 0);

public sealed record ToggleStageRequest(bool Complete);
