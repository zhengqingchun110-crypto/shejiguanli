using System.IO;
using DecorationProjectScheduler.App.Helpers;
using Microsoft.Data.Sqlite;

namespace DecorationProjectScheduler.App.Database;

public sealed class SqliteInitializer
{
    private readonly string _connectionString;

    public SqliteInitializer(string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        _connectionString = $"Data Source={databasePath}";
    }

    public string ConnectionString => _connectionString;

    public void Initialize()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Employees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Role TEXT NOT NULL,
                Department TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Projects (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectCode TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                Status TEXT NOT NULL,
                ManagerId INTEGER NOT NULL,
                Area TEXT NOT NULL DEFAULT '',
                ProjectType TEXT NOT NULL DEFAULT '',
                OperatorNames TEXT NOT NULL DEFAULT '',
                TaskPlan TEXT NOT NULL DEFAULT '',
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                Summary TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                Archived INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ProjectStages (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                StageName TEXT NOT NULL,
                SequenceNo INTEGER NOT NULL,
                PlannedDate TEXT NOT NULL,
                CompletedDate TEXT NULL,
                Status TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Tasks (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                OwnerId INTEGER NOT NULL,
                WorkloadPercent INTEGER NOT NULL,
                ProgressPercent INTEGER NOT NULL,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                Status TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SiteVisits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                VisitDate TEXT NOT NULL,
                Issues TEXT NOT NULL,
                Suggestions TEXT NOT NULL,
                PhotoPath TEXT NOT NULL,
                RectificationStatus TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS HandoverRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                HandoverDate TEXT NOT NULL,
                Participants TEXT NOT NULL,
                AttachmentPath TEXT NOT NULL,
                Notes TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS AcceptanceRecords (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                AcceptanceDate TEXT NOT NULL,
                Result TEXT NOT NULL,
                RectificationItems TEXT NOT NULL,
                ReviewRecord TEXT NOT NULL,
                Status TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProjectFiles (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                Category TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                UploadedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ProjectFollowUps (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProjectId INTEGER NOT NULL,
                TaskId INTEGER NULL,
                Content TEXT NOT NULL,
                OperatorName TEXT NOT NULL,
                CompletedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
        EnsureColumn(connection, "Projects", "Area", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Projects", "ProjectType", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Projects", "OperatorNames", "TEXT NOT NULL DEFAULT ''");
        EnsureColumn(connection, "Projects", "TaskPlan", "TEXT NOT NULL DEFAULT ''");
        NormalizeVisibleEnglish(connection);

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Projects;";
        var existing = Convert.ToInt32(countCommand.ExecuteScalar());
        if (existing == 0)
        {
            Seed(connection);
        }
    }

    private static void EnsureColumn(SqliteConnection connection, string tableName, string columnName, string definition)
    {
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = checkCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        alterCommand.ExecuteNonQuery();
    }

    private static void NormalizeVisibleEnglish(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Projects
            SET ProjectCode = REPLACE(REPLACE(ProjectCode, 'DEC-', ''), 'XM-', '')
            WHERE ProjectCode LIKE 'DEC-%' OR ProjectCode LIKE 'XM-%';

            UPDATE ProjectFiles
            SET Category = CASE
                    WHEN Category = 'CAD' THEN '图纸文件'
                    WHEN Category = 'SU' THEN '模型文件'
                    ELSE Category
                END,
                FilePath = REPLACE(REPLACE(FilePath, 'DEC-', ''), 'XM-', '')
            WHERE Category IN ('CAD', 'SU')
               OR FilePath LIKE '%DEC-%'
               OR FilePath LIKE '%XM-%';
            """;
        command.ExecuteNonQuery();
    }

    private static void Seed(SqliteConnection connection)
    {
        var now = DateTime.Today;
        using var transaction = connection.BeginTransaction();

        var employees = new (string Name, string Role, string Department)[]
        {
            ("林序", "设计总监", "设计中心"),
            ("苏楠", "主案设计师", "室内设计一部"),
            ("许洲", "深化设计师", "施工图组"),
            ("乔雨", "项目巡场", "工程支持部"),
            ("方宁", "资料专员", "项目管理部")
        };

        foreach (var employee in employees)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO Employees (Name, Role, Department)
                VALUES ($name, $role, $department);
                """;
            cmd.Parameters.AddWithValue("$name", employee.Name);
            cmd.Parameters.AddWithValue("$role", employee.Role);
            cmd.Parameters.AddWithValue("$department", employee.Department);
            cmd.ExecuteNonQuery();
        }

        var projects = new[]
        {
            new
            {
                Code = "2026001",
                Name = "滨江会所样板间项目",
                Status = "执行中",
                ManagerId = 2,
                StartDate = now.AddDays(-20),
                EndDate = now.AddDays(35),
                Summary = "样板间深化与现场配合并行推进，重点关注施工图交付与巡场整改。",
                UpdatedAt = DateTime.Now
            },
            new
            {
                Code = "2026002",
                Name = "望湖办公展厅升级",
                Status = "方案中",
                ManagerId = 1,
                StartDate = now.AddDays(-10),
                EndDate = now.AddDays(50),
                Summary = "展厅空间更新项目，当前聚焦平面方案和效果图确认。",
                UpdatedAt = DateTime.Now.AddHours(-6)
            },
            new
            {
                Code = "2026003",
                Name = "栖山别墅全案设计",
                Status = "巡场中",
                ManagerId = 2,
                StartDate = now.AddDays(-60),
                EndDate = now.AddDays(10),
                Summary = "进入施工巡场和竣工验收阶段，需紧盯收口质量与资料归档。",
                UpdatedAt = DateTime.Now.AddHours(-2)
            }
        };

        foreach (var project in projects)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO Projects (ProjectCode, Name, Status, ManagerId, StartDate, EndDate, Summary, UpdatedAt, Archived)
                VALUES ($code, $name, $status, $managerId, $startDate, $endDate, $summary, $updatedAt, 0);
                """;
            cmd.Parameters.AddWithValue("$code", project.Code);
            cmd.Parameters.AddWithValue("$name", project.Name);
            cmd.Parameters.AddWithValue("$status", project.Status);
            cmd.Parameters.AddWithValue("$managerId", project.ManagerId);
            cmd.Parameters.AddWithValue("$startDate", project.StartDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$endDate", project.EndDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$summary", project.Summary);
            cmd.Parameters.AddWithValue("$updatedAt", project.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        var projectIds = new Dictionary<string, int>();
        using (var readProjects = connection.CreateCommand())
        {
            readProjects.Transaction = transaction;
            readProjects.CommandText = "SELECT Id, ProjectCode FROM Projects;";
            using var reader = readProjects.ExecuteReader();
            while (reader.Read())
            {
                projectIds[reader.GetString(1)] = reader.GetInt32(0);
            }
        }

        foreach (var pair in projectIds)
        {
            for (var i = 0; i < DesignStageTemplates.DefaultStages.Length; i++)
            {
                var plannedDate = pair.Key switch
                {
                    "2026001" => now.AddDays(i * 4 - 8),
                    "2026002" => now.AddDays(i * 5 - 2),
                    _ => now.AddDays(i * 3 - 30)
                };

                var completedDate = pair.Key switch
                {
                    "2026001" when i < 4 => plannedDate.AddDays(-1),
                    "2026002" when i < 2 => plannedDate,
                    "2026003" when i < 7 => plannedDate.AddDays(1),
                    _ => (DateTime?)null
                };

                var status = completedDate.HasValue ? "已完成" : plannedDate < now ? "延期" : "待执行";

                using var stageCommand = connection.CreateCommand();
                stageCommand.Transaction = transaction;
                stageCommand.CommandText = """
                    INSERT INTO ProjectStages (ProjectId, StageName, SequenceNo, PlannedDate, CompletedDate, Status)
                    VALUES ($projectId, $stageName, $sequenceNo, $plannedDate, $completedDate, $status);
                    """;
                stageCommand.Parameters.AddWithValue("$projectId", pair.Value);
                stageCommand.Parameters.AddWithValue("$stageName", DesignStageTemplates.DefaultStages[i]);
                stageCommand.Parameters.AddWithValue("$sequenceNo", i + 1);
                stageCommand.Parameters.AddWithValue("$plannedDate", plannedDate.ToString("yyyy-MM-dd"));
                stageCommand.Parameters.AddWithValue("$completedDate", completedDate is null ? DBNull.Value : completedDate.Value.ToString("yyyy-MM-dd"));
                stageCommand.Parameters.AddWithValue("$status", status);
                stageCommand.ExecuteNonQuery();
            }
        }

        var tasks = new[]
        {
            new { ProjectCode = "2026001", Name = "施工图节点出图", OwnerId = 3, Workload = 45, Progress = 70, Start = now.AddDays(-6), End = now.AddDays(3), Status = "进行中" },
            new { ProjectCode = "2026001", Name = "巡场问题清单复核", OwnerId = 4, Workload = 30, Progress = 40, Start = now.AddDays(-2), End = now.AddDays(4), Status = "进行中" },
            new { ProjectCode = "2026002", Name = "平面方案二轮优化", OwnerId = 2, Workload = 35, Progress = 55, Start = now.AddDays(-3), End = now.AddDays(6), Status = "进行中" },
            new { ProjectCode = "2026002", Name = "效果图材质清单确认", OwnerId = 1, Workload = 25, Progress = 20, Start = now, End = now.AddDays(9), Status = "待开始" },
            new { ProjectCode = "2026003", Name = "现场收口巡查", OwnerId = 4, Workload = 55, Progress = 80, Start = now.AddDays(-4), End = now.AddDays(1), Status = "待验收" },
            new { ProjectCode = "2026003", Name = "竣工资料归档", OwnerId = 5, Workload = 25, Progress = 30, Start = now.AddDays(-1), End = now.AddDays(7), Status = "进行中" }
        };

        foreach (var task in tasks)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO Tasks (ProjectId, Name, OwnerId, WorkloadPercent, ProgressPercent, StartDate, EndDate, Status)
                VALUES ($projectId, $name, $ownerId, $workload, $progress, $startDate, $endDate, $status);
                """;
            cmd.Parameters.AddWithValue("$projectId", projectIds[task.ProjectCode]);
            cmd.Parameters.AddWithValue("$name", task.Name);
            cmd.Parameters.AddWithValue("$ownerId", task.OwnerId);
            cmd.Parameters.AddWithValue("$workload", task.Workload);
            cmd.Parameters.AddWithValue("$progress", task.Progress);
            cmd.Parameters.AddWithValue("$startDate", task.Start.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$endDate", task.End.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$status", task.Status);
            cmd.ExecuteNonQuery();
        }

        var visits = new[]
        {
            new { ProjectCode = "2026001", VisitDate = now.AddDays(-1), Issues = "木饰面收口缝偏大", Suggestions = "补充压条并调整拼缝", Status = "整改中" },
            new { ProjectCode = "2026003", VisitDate = now.AddDays(-2), Issues = "灯槽转角出光不均", Suggestions = "复核灯带规格与转角龙骨尺寸", Status = "待复验" }
        };

        foreach (var visit in visits)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO SiteVisits (ProjectId, VisitDate, Issues, Suggestions, PhotoPath, RectificationStatus)
                VALUES ($projectId, $visitDate, $issues, $suggestions, '', $status);
                """;
            cmd.Parameters.AddWithValue("$projectId", projectIds[visit.ProjectCode]);
            cmd.Parameters.AddWithValue("$visitDate", visit.VisitDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$issues", visit.Issues);
            cmd.Parameters.AddWithValue("$suggestions", visit.Suggestions);
            cmd.Parameters.AddWithValue("$status", visit.Status);
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO HandoverRecords (ProjectId, HandoverDate, Participants, AttachmentPath, Notes)
                VALUES ($projectId, $handoverDate, $participants, '', $notes);
                """;
            cmd.Parameters.AddWithValue("$projectId", projectIds["2026001"]);
            cmd.Parameters.AddWithValue("$handoverDate", now.AddDays(-5).ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$participants", "设计、工程、施工班组");
            cmd.Parameters.AddWithValue("$notes", "完成施工图交底和节点做法说明");
            cmd.ExecuteNonQuery();
        }

        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO AcceptanceRecords (ProjectId, AcceptanceDate, Result, RectificationItems, ReviewRecord, Status)
                VALUES ($projectId, $acceptanceDate, $result, $rectificationItems, $reviewRecord, $status);
                """;
            cmd.Parameters.AddWithValue("$projectId", projectIds["2026003"]);
            cmd.Parameters.AddWithValue("$acceptanceDate", now.AddDays(-3).ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$result", "初验通过");
            cmd.Parameters.AddWithValue("$rectificationItems", "窗帘盒补漆、收边硅胶重打");
            cmd.Parameters.AddWithValue("$reviewRecord", "计划三日后复验");
            cmd.Parameters.AddWithValue("$status", "待复验");
            cmd.ExecuteNonQuery();
        }

        var files = new[]
        {
            new { ProjectCode = "2026001", Category = "施工图", FileName = "会所样板间-施工图总包.dwg", FilePath = @"2026001\施工图\会所样板间-施工图总包.dwg" },
            new { ProjectCode = "2026003", Category = "巡场照片", FileName = "别墅样板层巡场-0601.jpg", FilePath = @"2026003\巡场照片\别墅样板层巡场-0601.jpg" }
        };

        foreach (var file in files)
        {
            using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO ProjectFiles (ProjectId, Category, FileName, FilePath, UploadedAt)
                VALUES ($projectId, $category, $fileName, $filePath, $uploadedAt);
                """;
            cmd.Parameters.AddWithValue("$projectId", projectIds[file.ProjectCode]);
            cmd.Parameters.AddWithValue("$category", file.Category);
            cmd.Parameters.AddWithValue("$fileName", file.FileName);
            cmd.Parameters.AddWithValue("$filePath", file.FilePath);
            cmd.Parameters.AddWithValue("$uploadedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }
}
