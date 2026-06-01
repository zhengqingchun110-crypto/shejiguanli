using DecorationProjectScheduler.Api;

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

app.MapGet("/", () => Results.Ok(new { status = "ok", name = "设计管理系统 API" }));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", time = DateTime.Now }));
app.MapGet("/api/update/latest", (IConfiguration configuration) =>
{
    var version = configuration["Update:Version"] ?? "1.0.1";
    var downloadUrl = configuration["Update:DownloadUrl"] ?? "http://47.116.74.183/downloads/DesignScheduler-CloudClient.zip";
    var notes = configuration["Update:Notes"] ?? "优化云端同步、在线状态和项目管理体验。";

    return Results.Ok(new
    {
        version,
        downloadUrl,
        notes
    });
});
app.MapGet("/api/workspace", (PostgresSchedulerRepository repo) => repo.GetSnapshotAsync());

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
app.MapPut("/api/stages/{stageId:int}/toggle", async (PostgresSchedulerRepository repo, int stageId, ToggleStageRequest request) =>
{
    await repo.ToggleStageAsync(stageId, request.Complete);
    return Results.Ok();
});

app.Run();
