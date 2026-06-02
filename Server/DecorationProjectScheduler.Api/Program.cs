using DecorationProjectScheduler.Api;

using DecorationProjectScheduler.App.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<PostgresSchedulerRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

var repository = app.Services.GetRequiredService<PostgresSchedulerRepository>();
await repository.InitializeAsync();

app.MapGet("/", () => Results.Ok(new { status = "ok", name = "凡响智道 API" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.Now }));
app.MapGet("/api/update/latest", (IConfiguration configuration) =>
{
    var version = configuration["Update:Version"] ?? "1.0.13";
    var downloadUrl = configuration["Update:DownloadUrl"] ?? "http://47.116.74.183/downloads/DesignScheduler-CloudClient.zip";
    var notes = configuration["Update:Notes"] ?? "1.0.13 资料中心更新：修复复制云盘链接时可能崩溃的问题，调整云端存储展示位置，新增上传资料文件大小显示，并升级云端文件大小字段。";

    return Results.Ok(new
    {
        version,
        downloadUrl,
        notes
    });
});
app.MapGet("/api/workspace", (PostgresSchedulerRepository repo) => repo.GetSnapshotAsync());
app.MapGet("/api/storage", (IWebHostEnvironment environment) =>
{
    var storageRoot = Path.Combine(environment.ContentRootPath, "ProjectFiles");
    Directory.CreateDirectory(storageRoot);
    var rootPath = Path.GetFullPath(storageRoot);
    var driveRoot = Path.GetPathRoot(rootPath) ?? rootPath;
    var drive = new DriveInfo(driveRoot);

    return Results.Ok(new CloudStorageStatus
    {
        IsCloud = true,
        AvailableBytes = drive.AvailableFreeSpace,
        TotalBytes = drive.TotalSize,
        CheckedAt = DateTime.Now
    });
});

app.MapPost("/api/employees", async (PostgresSchedulerRepository repo, AddEmployeeRequest request) =>
{
    await repo.AddEmployeeAsync(request);
    return Results.Ok();
});
app.MapDelete("/api/employees/{employeeId:int}", async (PostgresSchedulerRepository repo, int employeeId) =>
{
    await repo.DeleteEmployeeAsync(employeeId);
    return Results.Ok();
});

app.MapPost("/api/projects", async (PostgresSchedulerRepository repo, CreateProjectRequest request) =>
{
    await repo.CreateProjectAsync(request);
    return Results.Ok();
});
app.MapPut("/api/projects/{projectId:int}", async (PostgresSchedulerRepository repo, int projectId, UpdateProjectRequest request) =>
{
    await repo.UpdateProjectAsync(projectId, request);
    return Results.Ok();
});
app.MapDelete("/api/projects/{projectId:int}", async (PostgresSchedulerRepository repo, int projectId) =>
{
    await repo.DeleteProjectAsync(projectId);
    return Results.Ok();
});

app.MapPost("/api/tasks", async (PostgresSchedulerRepository repo, AddTaskRequest request) =>
{
    await repo.AddTaskAsync(request);
    return Results.Ok();
});
app.MapPut("/api/tasks/{taskId:int}", async (PostgresSchedulerRepository repo, int taskId, UpdateTaskRequest request) =>
{
    await repo.UpdateTaskAsync(taskId, request);
    return Results.Ok();
});
app.MapPut("/api/tasks/{taskId:int}/schedule", async (PostgresSchedulerRepository repo, int taskId, UpdateTaskScheduleRequest request) =>
{
    await repo.UpdateTaskScheduleAsync(taskId, request);
    return Results.Ok();
});
app.MapPost("/api/tasks/{taskId:int}/complete", async (PostgresSchedulerRepository repo, int taskId) =>
{
    await repo.CompleteTaskWithFollowUpAsync(taskId);
    return Results.Ok();
});
app.MapDelete("/api/tasks/{taskId:int}", async (PostgresSchedulerRepository repo, int taskId) =>
{
    await repo.DeleteTaskAsync(taskId);
    return Results.Ok();
});

app.MapPost("/api/follow-ups", async (PostgresSchedulerRepository repo, AddProjectFollowUpRequest request) =>
{
    await repo.AddProjectFollowUpAsync(request);
    return Results.Ok();
});
app.MapDelete("/api/follow-ups/{followUpId:int}", async (PostgresSchedulerRepository repo, int followUpId) =>
{
    await repo.DeleteProjectFollowUpAsync(followUpId);
    return Results.Ok();
});

app.MapPost("/api/site-visits", async (PostgresSchedulerRepository repo, AddSiteVisitRequest request) =>
{
    await repo.AddSiteVisitAsync(request);
    return Results.Ok();
});
app.MapPost("/api/acceptances", async (PostgresSchedulerRepository repo, AddAcceptanceRequest request) =>
{
    await repo.AddAcceptanceAsync(request);
    return Results.Ok();
});
app.MapPost("/api/files", async (PostgresSchedulerRepository repo, AddProjectFileRequest request) =>
{
    await repo.AddProjectFileAsync(request);
    return Results.Ok();
});
app.MapPost("/api/files/upload", async (PostgresSchedulerRepository repo, IWebHostEnvironment environment, HttpRequest request) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest("缺少上传文件。");
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
    {
        return Results.BadRequest("请选择需要上传的文件。");
    }

    if (!int.TryParse(form["projectId"], out var projectId))
    {
        return Results.BadRequest("项目编号无效。");
    }

    var projectCode = SanitizePathPart(form["projectCode"].ToString());
    var category = form["category"].ToString();
    var safeCategory = SanitizePathPart(category);
    var safeFileName = SanitizeFileName(file.FileName);
    var uploadsRoot = Path.Combine(environment.ContentRootPath, "ProjectFiles", projectCode, safeCategory);
    Directory.CreateDirectory(uploadsRoot);

    var storedFileName = $"{DateTime.Now:yyyyMMddHHmmssfff}-{safeFileName}";
    var storedPath = Path.Combine(uploadsRoot, storedFileName);
    await using (var stream = File.Create(storedPath))
    {
        await file.CopyToAsync(stream);
    }

    var fileId = await repo.AddProjectFileAndReturnIdAsync(new AddProjectFileRequest(projectId, category, file.FileName, storedPath, file.Length));
    return Results.Ok(new { id = fileId });
});
app.MapGet("/api/files/{fileId:int}/download", async (PostgresSchedulerRepository repo, int fileId) =>
{
    var file = await repo.GetProjectFileAsync(fileId);
    if (file is null || !File.Exists(file.FilePath))
    {
        return Results.NotFound("文件不存在。");
    }

    return Results.File(file.FilePath, "application/octet-stream", file.FileName);
});
app.MapDelete("/api/files/{fileId:int}", async (PostgresSchedulerRepository repo, int fileId) =>
{
    var file = await repo.DeleteProjectFileAsync(fileId);
    if (file is null)
    {
        return Results.NotFound();
    }

    if (File.Exists(file.FilePath))
    {
        try
        {
            File.Delete(file.FilePath);
        }
        catch
        {
            // 文件可能被系统占用，数据库记录已删除，后续可由服务器清理任务处理。
        }
    }

    return Results.Ok();
});
app.MapPut("/api/stages/{stageId:int}/toggle", async (PostgresSchedulerRepository repo, int stageId, ToggleStageRequest request) =>
{
    await repo.ToggleStageAsync(stageId, request.Complete);
    return Results.Ok();
});

app.Run();

static string SanitizePathPart(string value)
{
    var fallback = string.IsNullOrWhiteSpace(value) ? "未分类" : value.Trim();
    foreach (var invalid in Path.GetInvalidFileNameChars())
    {
        fallback = fallback.Replace(invalid, '_');
    }

    return fallback;
}

static string SanitizeFileName(string value)
{
    var fileName = Path.GetFileName(value);
    return SanitizePathPart(fileName);
}
