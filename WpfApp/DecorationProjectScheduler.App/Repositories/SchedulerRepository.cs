using DecorationProjectScheduler.App.Helpers;
using DecorationProjectScheduler.App.Models;
using System.IO;
using DecorationProjectScheduler.App.Services;
using Microsoft.Data.Sqlite;

namespace DecorationProjectScheduler.App.Repositories;

public sealed class SchedulerRepository : ISchedulerRepository
{
    private readonly string _connectionString;
    private readonly FileStorageService _fileStorageService;

    public SchedulerRepository(string connectionString, FileStorageService fileStorageService)
    {
        _connectionString = connectionString;
        _fileStorageService = fileStorageService;
    }

    public event EventHandler? DataChanged;

    public bool IsCloudMode => false;

    public bool TestConnection() => true;

    public void AddEmployee(string name, string role, string department)
    {
        ExecuteNonQuery("""
            INSERT INTO Employees (Name, Role, Department)
            VALUES ($name, $role, $department);
            """,
            ("$name", name),
            ("$role", role),
            ("$department", department));

        RaiseChanged();
    }

    public void DeleteEmployee(int employeeId)
    {
        ExecuteNonQuery("""
            DELETE FROM Tasks
            WHERE OwnerId = $employeeId;

            DELETE FROM Employees
            WHERE Id = $employeeId;
            """,
            ("$employeeId", employeeId));

        RaiseChanged();
    }

    public WorkspaceSnapshot GetSnapshot()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        return new WorkspaceSnapshot
        {
            Employees = ReadEmployees(connection),
            Projects = ReadProjects(connection),
            ProjectStages = ReadStages(connection),
            Tasks = ReadTasks(connection),
            SiteVisits = ReadSiteVisits(connection),
            HandoverRecords = ReadHandoverRecords(connection),
            AcceptanceRecords = ReadAcceptanceRecords(connection),
            ProjectFiles = ReadProjectFiles(connection),
            ProjectFollowUps = ReadProjectFollowUps(connection),
        };
    }

    public void CreateProject(string name, string area, string projectType, int managerId, DateTime startDate, DateTime endDate, string summary)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var code = $"{DateTime.Today:yyyy}{GetNextProjectSequence(connection, transaction):000}";

        using var projectCommand = connection.CreateCommand();
        projectCommand.Transaction = transaction;
        projectCommand.CommandText = """
            INSERT INTO Projects (ProjectCode, Name, Status, ManagerId, Area, ProjectType, OperatorNames, TaskPlan, StartDate, EndDate, Summary, UpdatedAt, Archived)
            VALUES ($code, $name, '规划中', $managerId, $area, $projectType, '', '', $startDate, $endDate, $summary, $updatedAt, 0);
            SELECT last_insert_rowid();
            """;
        projectCommand.Parameters.AddWithValue("$code", code);
        projectCommand.Parameters.AddWithValue("$name", name);
        projectCommand.Parameters.AddWithValue("$managerId", managerId);
        projectCommand.Parameters.AddWithValue("$area", area);
        projectCommand.Parameters.AddWithValue("$projectType", projectType);
        projectCommand.Parameters.AddWithValue("$startDate", startDate.ToString("yyyy-MM-dd"));
        projectCommand.Parameters.AddWithValue("$endDate", endDate.ToString("yyyy-MM-dd"));
        projectCommand.Parameters.AddWithValue("$summary", summary);
        projectCommand.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        var projectId = Convert.ToInt32(projectCommand.ExecuteScalar());

        var totalDays = Math.Max((endDate - startDate).Days, DesignStageTemplates.DefaultStages.Length);
        for (var i = 0; i < DesignStageTemplates.DefaultStages.Length; i++)
        {
            var plannedDate = startDate.AddDays(i * totalDays / DesignStageTemplates.DefaultStages.Length);
            using var stageCommand = connection.CreateCommand();
            stageCommand.Transaction = transaction;
            stageCommand.CommandText = """
                INSERT INTO ProjectStages (ProjectId, StageName, SequenceNo, PlannedDate, CompletedDate, Status)
                VALUES ($projectId, $stageName, $sequenceNo, $plannedDate, NULL, '待执行');
                """;
            stageCommand.Parameters.AddWithValue("$projectId", projectId);
            stageCommand.Parameters.AddWithValue("$stageName", DesignStageTemplates.DefaultStages[i]);
            stageCommand.Parameters.AddWithValue("$sequenceNo", i + 1);
            stageCommand.Parameters.AddWithValue("$plannedDate", plannedDate.ToString("yyyy-MM-dd"));
            stageCommand.ExecuteNonQuery();
        }

        transaction.Commit();
        RaiseChanged();
    }

    public void UpdateProject(int projectId, string name, string area, string projectType, string operatorNames, string summary, string taskPlan)
    {
        ExecuteNonQuery("""
            UPDATE Projects
            SET Name = $name,
                Area = $area,
                ProjectType = $projectType,
                OperatorNames = $operatorNames,
                Summary = $summary,
                TaskPlan = $taskPlan,
                UpdatedAt = $updatedAt
            WHERE Id = $projectId;
            """,
            ("$projectId", projectId),
            ("$name", name),
            ("$area", area),
            ("$projectType", projectType),
            ("$operatorNames", operatorNames),
            ("$summary", summary),
            ("$taskPlan", taskPlan),
            ("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

        RaiseChanged();
    }

    public void DeleteProject(int projectId)
    {
        ExecuteNonQuery("""
            DELETE FROM ProjectFollowUps WHERE ProjectId = $projectId;
            DELETE FROM ProjectFiles WHERE ProjectId = $projectId;
            DELETE FROM AcceptanceRecords WHERE ProjectId = $projectId;
            DELETE FROM HandoverRecords WHERE ProjectId = $projectId;
            DELETE FROM SiteVisits WHERE ProjectId = $projectId;
            DELETE FROM Tasks WHERE ProjectId = $projectId;
            DELETE FROM ProjectStages WHERE ProjectId = $projectId;
            DELETE FROM Projects WHERE Id = $projectId;
            """,
            ("$projectId", projectId));

        RaiseChanged();
    }

    public void AddTask(int projectId, string name, int ownerId, int workloadPercent, int progressPercent, DateTime startDate, DateTime endDate, string status)
    {
        ExecuteNonQuery("""
            INSERT INTO Tasks (ProjectId, Name, OwnerId, WorkloadPercent, ProgressPercent, StartDate, EndDate, Status)
            VALUES ($projectId, $name, $ownerId, $workload, $progress, $startDate, $endDate, $status);
            """,
            ("$projectId", projectId),
            ("$name", name),
            ("$ownerId", ownerId),
            ("$workload", workloadPercent),
            ("$progress", progressPercent),
            ("$startDate", startDate.ToString("yyyy-MM-dd")),
            ("$endDate", endDate.ToString("yyyy-MM-dd")),
            ("$status", status));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void UpdateTask(int taskId, string name)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        int projectId;
        using (var query = connection.CreateCommand())
        {
            query.CommandText = "SELECT ProjectId FROM Tasks WHERE Id = $taskId;";
            query.Parameters.AddWithValue("$taskId", taskId);
            projectId = Convert.ToInt32(query.ExecuteScalar());
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                UPDATE Tasks
                SET Name = $name
                WHERE Id = $taskId;
                """;
            command.Parameters.AddWithValue("$taskId", taskId);
            command.Parameters.AddWithValue("$name", name);
            command.ExecuteNonQuery();
        }

        TouchProject(projectId);
        RaiseChanged();
    }

    public void UpdateTaskFromSchedule(int taskId, int projectId, string name, DateTime endDate)
    {
        int previousProjectId;
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT ProjectId FROM Tasks WHERE Id = $taskId;";
            query.Parameters.AddWithValue("$taskId", taskId);
            previousProjectId = Convert.ToInt32(query.ExecuteScalar());
        }

        ExecuteNonQuery("""
            UPDATE Tasks
            SET ProjectId = $projectId,
                Name = $name,
                EndDate = $endDate,
                Status = CASE WHEN ProgressPercent >= 100 THEN '已完成' ELSE Status END
            WHERE Id = $taskId;
            """,
            ("$taskId", taskId),
            ("$projectId", projectId),
            ("$name", name),
            ("$endDate", endDate.ToString("yyyy-MM-dd")));

        TouchProject(previousProjectId);
        TouchProject(projectId);
        RaiseChanged();
    }

    public void DeleteTask(int taskId)
    {
        int projectId;
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT ProjectId FROM Tasks WHERE Id = $taskId;";
            query.Parameters.AddWithValue("$taskId", taskId);
            projectId = Convert.ToInt32(query.ExecuteScalar());
        }

        ExecuteNonQuery("""
            DELETE FROM Tasks
            WHERE Id = $taskId;
            """,
            ("$taskId", taskId));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void CompleteTask(int taskId)
    {
        int projectId;
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT ProjectId FROM Tasks WHERE Id = $taskId;";
            query.Parameters.AddWithValue("$taskId", taskId);
            projectId = Convert.ToInt32(query.ExecuteScalar());
        }

        ExecuteNonQuery("""
            UPDATE Tasks
            SET ProgressPercent = 100,
                Status = '已完成'
            WHERE Id = $taskId;
            """,
            ("$taskId", taskId));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void CompleteTaskWithFollowUp(int taskId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        int projectId;
        string taskName;
        string operatorName;
        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText = """
                SELECT Tasks.ProjectId, Tasks.Name, Employees.Name
                FROM Tasks
                LEFT JOIN Employees ON Employees.Id = Tasks.OwnerId
                WHERE Tasks.Id = $taskId;
                """;
            query.Parameters.AddWithValue("$taskId", taskId);
            using var reader = query.ExecuteReader();
            if (!reader.Read())
            {
                return;
            }

            projectId = reader.GetInt32(0);
            taskName = reader.GetString(1);
            operatorName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
        }

        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE Tasks
                SET ProgressPercent = 100,
                    Status = '已完成'
                WHERE Id = $taskId;
                """;
            update.Parameters.AddWithValue("$taskId", taskId);
            update.ExecuteNonQuery();
        }

        InsertFollowUp(connection, transaction, projectId, taskId, taskName, operatorName, DateTime.Now);
        TouchProject(connection, transaction, projectId);
        transaction.Commit();
        RaiseChanged();
    }

    public void AddProjectFollowUp(int projectId, int? taskId, string content, string operatorName, DateTime completedAt)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        InsertFollowUp(connection, transaction, projectId, taskId, content, operatorName, completedAt);
        TouchProject(connection, transaction, projectId);
        transaction.Commit();
        RaiseChanged();
    }

    public void DeleteProjectFollowUp(int followUpId)
    {
        int projectId;
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT ProjectId FROM ProjectFollowUps WHERE Id = $followUpId;";
            query.Parameters.AddWithValue("$followUpId", followUpId);
            var value = query.ExecuteScalar();
            if (value is null)
            {
                return;
            }

            projectId = Convert.ToInt32(value);
        }

        ExecuteNonQuery("""
            DELETE FROM ProjectFollowUps
            WHERE Id = $followUpId;
            """,
            ("$followUpId", followUpId));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void AddSiteVisit(int projectId, DateTime visitDate, string issues, string suggestions, string photoPath, string rectificationStatus)
    {
        ExecuteNonQuery("""
            INSERT INTO SiteVisits (ProjectId, VisitDate, Issues, Suggestions, PhotoPath, RectificationStatus)
            VALUES ($projectId, $visitDate, $issues, $suggestions, $photoPath, $rectificationStatus);
            """,
            ("$projectId", projectId),
            ("$visitDate", visitDate.ToString("yyyy-MM-dd")),
            ("$issues", issues),
            ("$suggestions", suggestions),
            ("$photoPath", photoPath),
            ("$rectificationStatus", rectificationStatus));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void AddHandoverRecord(int projectId, DateTime handoverDate, string participants, string attachmentPath, string notes)
    {
        ExecuteNonQuery("""
            INSERT INTO HandoverRecords (ProjectId, HandoverDate, Participants, AttachmentPath, Notes)
            VALUES ($projectId, $handoverDate, $participants, $attachmentPath, $notes);
            """,
            ("$projectId", projectId),
            ("$handoverDate", handoverDate.ToString("yyyy-MM-dd")),
            ("$participants", participants),
            ("$attachmentPath", attachmentPath),
            ("$notes", notes));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void AddAcceptanceRecord(int projectId, DateTime acceptanceDate, string result, string rectificationItems, string reviewRecord, string status)
    {
        ExecuteNonQuery("""
            INSERT INTO AcceptanceRecords (ProjectId, AcceptanceDate, Result, RectificationItems, ReviewRecord, Status)
            VALUES ($projectId, $acceptanceDate, $result, $rectificationItems, $reviewRecord, $status);
            """,
            ("$projectId", projectId),
            ("$acceptanceDate", acceptanceDate.ToString("yyyy-MM-dd")),
            ("$result", result),
            ("$rectificationItems", rectificationItems),
            ("$reviewRecord", reviewRecord),
            ("$status", status));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void AddProjectFile(int projectId, string category, string fileName, string filePath)
    {
        ExecuteNonQuery("""
            INSERT INTO ProjectFiles (ProjectId, Category, FileName, FilePath, UploadedAt)
            VALUES ($projectId, $category, $fileName, $filePath, $uploadedAt);
            """,
            ("$projectId", projectId),
            ("$category", category),
            ("$fileName", fileName),
            ("$filePath", filePath),
            ("$uploadedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

        TouchProject(projectId);
        RaiseChanged();
    }

    public void UploadProjectFile(int projectId, string projectCode, string category, string sourcePath)
    {
        var savedPath = _fileStorageService.SaveProjectFile(projectCode, category, sourcePath);
        AddProjectFile(projectId, category, Path.GetFileName(sourcePath), savedPath);
    }

    public void DownloadProjectFile(ProjectFileRecord file, string destinationPath)
    {
        if (!File.Exists(file.FilePath))
        {
            throw new FileNotFoundException("没有找到本地资料文件。", file.FilePath);
        }

        File.Copy(file.FilePath, destinationPath, overwrite: true);
    }

    public void DeleteProjectFile(int fileId)
    {
        string? filePath = null;
        int? projectId = null;
        using (var connection = new SqliteConnection(_connectionString))
        {
            connection.Open();
            using var query = connection.CreateCommand();
            query.CommandText = "SELECT ProjectId, FilePath FROM ProjectFiles WHERE Id = $fileId;";
            query.Parameters.AddWithValue("$fileId", fileId);
            using var reader = query.ExecuteReader();
            if (reader.Read())
            {
                projectId = reader.GetInt32(0);
                filePath = reader.GetString(1);
            }
        }

        ExecuteNonQuery("DELETE FROM ProjectFiles WHERE Id = $fileId;", ("$fileId", fileId));
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // 文件可能被占用，删除数据库记录优先保证界面状态正确。
            }
        }

        if (projectId is not null)
        {
            TouchProject(projectId.Value);
        }

        RaiseChanged();
    }

    public void ToggleStageCompletion(int stageId, bool complete)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        int projectId;
        using (var query = connection.CreateCommand())
        {
            query.CommandText = "SELECT ProjectId FROM ProjectStages WHERE Id = $id;";
            query.Parameters.AddWithValue("$id", stageId);
            projectId = Convert.ToInt32(query.ExecuteScalar());
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE ProjectStages
            SET CompletedDate = $completedDate,
                Status = $status
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", stageId);
        command.Parameters.AddWithValue("$completedDate", complete ? DateTime.Today.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("$status", complete ? "已完成" : "待执行");
        command.ExecuteNonQuery();

        TouchProject(projectId);
        RaiseChanged();
    }

    private void TouchProject(int projectId)
    {
        ExecuteNonQuery("""
            UPDATE Projects
            SET UpdatedAt = $updatedAt
            WHERE Id = $projectId;
            """,
            ("$projectId", projectId),
            ("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
    }

    private static void TouchProject(SqliteConnection connection, SqliteTransaction transaction, int projectId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE Projects
            SET UpdatedAt = $updatedAt
            WHERE Id = $projectId;
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    private static void InsertFollowUp(SqliteConnection connection, SqliteTransaction transaction, int projectId, int? taskId, string content, string operatorName, DateTime completedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ProjectFollowUps (ProjectId, TaskId, Content, OperatorName, CompletedAt)
            VALUES ($projectId, $taskId, $content, $operatorName, $completedAt);
            """;
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$taskId", taskId is null ? DBNull.Value : taskId.Value);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$operatorName", operatorName);
        command.Parameters.AddWithValue("$completedAt", completedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    private void ExecuteNonQuery(string sql, params (string Name, object? Value)[] parameters)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }

    private static int GetNextProjectSequence(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM Projects;";
        return Convert.ToInt32(command.ExecuteScalar()) + 1;
    }

    private static List<Employee> ReadEmployees(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Role, Department FROM Employees ORDER BY Id;";
        using var reader = command.ExecuteReader();
        var list = new List<Employee>();
        while (reader.Read())
        {
            list.Add(new Employee
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Role = reader.GetString(2),
                Department = reader.GetString(3)
            });
        }

        return list;
    }

    private static List<Project> ReadProjects(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectCode, Name, Status, ManagerId, Area, ProjectType, OperatorNames, TaskPlan, StartDate, EndDate, Summary, UpdatedAt, Archived
            FROM Projects
            ORDER BY UpdatedAt DESC;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<Project>();
        while (reader.Read())
        {
            list.Add(new Project
            {
                Id = reader.GetInt32(0),
                ProjectCode = reader.GetString(1),
                Name = reader.GetString(2),
                Status = reader.GetString(3),
                ManagerId = reader.GetInt32(4),
                Area = reader.GetString(5),
                ProjectType = reader.GetString(6),
                OperatorNames = reader.GetString(7),
                TaskPlan = reader.GetString(8),
                StartDate = DateTime.Parse(reader.GetString(9)),
                EndDate = DateTime.Parse(reader.GetString(10)),
                Summary = reader.GetString(11),
                UpdatedAt = DateTime.Parse(reader.GetString(12)),
                Archived = reader.GetInt32(13) == 1
            });
        }

        return list;
    }

    private static List<ProjectStage> ReadStages(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, StageName, SequenceNo, PlannedDate, CompletedDate, Status
            FROM ProjectStages
            ORDER BY ProjectId, SequenceNo;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<ProjectStage>();
        while (reader.Read())
        {
            list.Add(new ProjectStage
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                StageName = reader.GetString(2),
                SequenceNo = reader.GetInt32(3),
                PlannedDate = DateTime.Parse(reader.GetString(4)),
                CompletedDate = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                Status = reader.GetString(6)
            });
        }

        return list;
    }

    private static List<WorkTask> ReadTasks(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, Name, OwnerId, WorkloadPercent, ProgressPercent, StartDate, EndDate, Status
            FROM Tasks
            ORDER BY EndDate;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<WorkTask>();
        while (reader.Read())
        {
            list.Add(new WorkTask
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                Name = reader.GetString(2),
                OwnerId = reader.GetInt32(3),
                WorkloadPercent = reader.GetInt32(4),
                ProgressPercent = reader.GetInt32(5),
                StartDate = DateTime.Parse(reader.GetString(6)),
                EndDate = DateTime.Parse(reader.GetString(7)),
                Status = reader.GetString(8)
            });
        }

        return list;
    }

    private static List<SiteVisit> ReadSiteVisits(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, VisitDate, Issues, Suggestions, PhotoPath, RectificationStatus
            FROM SiteVisits
            ORDER BY VisitDate DESC;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<SiteVisit>();
        while (reader.Read())
        {
            list.Add(new SiteVisit
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                VisitDate = DateTime.Parse(reader.GetString(2)),
                Issues = reader.GetString(3),
                Suggestions = reader.GetString(4),
                PhotoPath = reader.GetString(5),
                RectificationStatus = reader.GetString(6)
            });
        }

        return list;
    }

    private static List<HandoverRecord> ReadHandoverRecords(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, HandoverDate, Participants, AttachmentPath, Notes
            FROM HandoverRecords
            ORDER BY HandoverDate DESC;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<HandoverRecord>();
        while (reader.Read())
        {
            list.Add(new HandoverRecord
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                HandoverDate = DateTime.Parse(reader.GetString(2)),
                Participants = reader.GetString(3),
                AttachmentPath = reader.GetString(4),
                Notes = reader.GetString(5)
            });
        }

        return list;
    }

    private static List<AcceptanceRecord> ReadAcceptanceRecords(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, AcceptanceDate, Result, RectificationItems, ReviewRecord, Status
            FROM AcceptanceRecords
            ORDER BY AcceptanceDate DESC;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<AcceptanceRecord>();
        while (reader.Read())
        {
            list.Add(new AcceptanceRecord
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                AcceptanceDate = DateTime.Parse(reader.GetString(2)),
                Result = reader.GetString(3),
                RectificationItems = reader.GetString(4),
                ReviewRecord = reader.GetString(5),
                Status = reader.GetString(6)
            });
        }

        return list;
    }

    private static List<ProjectFileRecord> ReadProjectFiles(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, Category, FileName, FilePath, UploadedAt
            FROM ProjectFiles
            ORDER BY UploadedAt DESC;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<ProjectFileRecord>();
        while (reader.Read())
        {
            list.Add(new ProjectFileRecord
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                Category = reader.GetString(2),
                FileName = reader.GetString(3),
                FilePath = reader.GetString(4),
                UploadedAt = DateTime.Parse(reader.GetString(5))
            });
        }

        return list;
    }

    private static List<ProjectFollowUp> ReadProjectFollowUps(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, ProjectId, TaskId, Content, OperatorName, CompletedAt
            FROM ProjectFollowUps
            ORDER BY CompletedAt DESC;
            """;
        using var reader = command.ExecuteReader();
        var list = new List<ProjectFollowUp>();
        while (reader.Read())
        {
            list.Add(new ProjectFollowUp
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                TaskId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Content = reader.GetString(3),
                OperatorName = reader.GetString(4),
                CompletedAt = DateTime.Parse(reader.GetString(5))
            });
        }

        return list;
    }

    private void RaiseChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}

