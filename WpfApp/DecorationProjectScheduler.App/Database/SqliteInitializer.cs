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
                FileSizeBytes INTEGER NOT NULL DEFAULT 0,
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
        EnsureColumn(connection, "ProjectFiles", "FileSizeBytes", "INTEGER NOT NULL DEFAULT 0");
        NormalizeVisibleEnglish(connection);

        // 正式版只负责建表和补字段，不能在更新或换目录时自动写入示例人员/项目。
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

}
