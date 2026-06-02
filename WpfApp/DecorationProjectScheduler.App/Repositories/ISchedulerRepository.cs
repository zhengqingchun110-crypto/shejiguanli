using DecorationProjectScheduler.App.Models;

namespace DecorationProjectScheduler.App.Repositories;

public interface ISchedulerRepository
{
    event EventHandler? DataChanged;

    bool IsCloudMode { get; }

    WorkspaceSnapshot GetSnapshot();
    bool TestConnection();
    void AddEmployee(string name, string role, string department);
    void DeleteEmployee(int employeeId);
    void CreateProject(string name, string area, string projectType, int managerId, DateTime startDate, DateTime endDate, string summary);
    void UpdateProject(int projectId, string name, string area, string projectType, string operatorNames, string summary, string taskPlan);
    void DeleteProject(int projectId);
    void AddTask(int projectId, string name, int ownerId, int workloadPercent, int progressPercent, DateTime startDate, DateTime endDate, string status);
    void UpdateTask(int taskId, string name);
    void UpdateTaskFromSchedule(int taskId, int projectId, string name, DateTime endDate);
    void DeleteTask(int taskId);
    void CompleteTask(int taskId);
    void CompleteTaskWithFollowUp(int taskId);
    void AddProjectFollowUp(int projectId, int? taskId, string content, string operatorName, DateTime completedAt);
    void DeleteProjectFollowUp(int followUpId);
    void AddSiteVisit(int projectId, DateTime visitDate, string issues, string suggestions, string photoPath, string rectificationStatus);
    void AddHandoverRecord(int projectId, DateTime handoverDate, string participants, string attachmentPath, string notes);
    void AddAcceptanceRecord(int projectId, DateTime acceptanceDate, string result, string rectificationItems, string reviewRecord, string status);
    void AddProjectFile(int projectId, string category, string fileName, string filePath);
    void UploadProjectFile(int projectId, string projectCode, string category, string sourcePath);
    void DownloadProjectFile(ProjectFileRecord file, string destinationPath);
    void DeleteProjectFile(int fileId);
    CloudStorageStatus GetCloudStorageStatus();
    void ToggleStageCompletion(int stageId, bool complete);
}
