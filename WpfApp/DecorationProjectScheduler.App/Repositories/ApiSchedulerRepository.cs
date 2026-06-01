using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using DecorationProjectScheduler.App.Models;

namespace DecorationProjectScheduler.App.Repositories;

public sealed class ApiSchedulerRepository : ISchedulerRepository, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApiSchedulerRepository(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
        };
    }

    public event EventHandler? DataChanged;

    public bool IsCloudMode => true;

    public WorkspaceSnapshot GetSnapshot() =>
        Send(() => _httpClient.GetFromJsonAsync<WorkspaceSnapshot>("api/workspace", _jsonOptions)) ?? new WorkspaceSnapshot();

    public bool TestConnection()
    {
        try
        {
            using var response = _httpClient.GetAsync("api/health").GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void AddEmployee(string name, string role, string department) =>
        Post("api/employees", new { name, role, department });

    public void DeleteEmployee(int employeeId) =>
        Delete($"api/employees/{employeeId}");

    public void CreateProject(string name, string area, string projectType, int managerId, DateTime startDate, DateTime endDate, string summary) =>
        Post("api/projects", new { name, area, projectType, managerId, startDate = ToDateOnlyText(startDate), endDate = ToDateOnlyText(endDate), summary });

    public void UpdateProject(int projectId, string name, string area, string projectType, string operatorNames, string summary, string taskPlan) =>
        Put($"api/projects/{projectId}", new { name, area, projectType, operatorNames, summary, taskPlan });

    public void DeleteProject(int projectId) =>
        Delete($"api/projects/{projectId}");

    public void AddTask(int projectId, string name, int ownerId, int workloadPercent, int progressPercent, DateTime startDate, DateTime endDate, string status) =>
        Post("api/tasks", new { projectId, name, ownerId, workloadPercent, progressPercent, startDate = ToDateOnlyText(startDate), endDate = ToDateOnlyText(endDate), status });

    public void UpdateTask(int taskId, string name) =>
        Put($"api/tasks/{taskId}", new { name });

    public void UpdateTaskFromSchedule(int taskId, int projectId, string name, DateTime endDate) =>
        Put($"api/tasks/{taskId}/schedule", new { projectId, name, endDate = ToDateOnlyText(endDate) });

    public void DeleteTask(int taskId) =>
        Delete($"api/tasks/{taskId}");

    public void CompleteTask(int taskId) =>
        Post($"api/tasks/{taskId}/complete", new { });

    public void CompleteTaskWithFollowUp(int taskId) =>
        Post($"api/tasks/{taskId}/complete", new { });

    public void AddProjectFollowUp(int projectId, int? taskId, string content, string operatorName, DateTime completedAt) =>
        Post("api/follow-ups", new { projectId, taskId, content, operatorName, completedAt = ToDateTimeText(completedAt) });

    public void DeleteProjectFollowUp(int followUpId) =>
        Delete($"api/follow-ups/{followUpId}");

    public void AddSiteVisit(int projectId, DateTime visitDate, string issues, string suggestions, string photoPath, string rectificationStatus) =>
        Post("api/site-visits", new { projectId, visitDate = ToDateOnlyText(visitDate), issues, suggestions, photoPath, rectificationStatus });

    public void AddHandoverRecord(int projectId, DateTime handoverDate, string participants, string attachmentPath, string notes)
    {
        // 当前界面暂未使用交底新增，保留接口占位，避免联网版调用失败。
        RaiseChanged();
    }

    public void AddAcceptanceRecord(int projectId, DateTime acceptanceDate, string result, string rectificationItems, string reviewRecord, string status) =>
        Post("api/acceptances", new { projectId, acceptanceDate = ToDateOnlyText(acceptanceDate), result, rectificationItems, reviewRecord, status });

    public void AddProjectFile(int projectId, string category, string fileName, string filePath) =>
        Post("api/files", new { projectId, category, fileName, filePath });

    public void ToggleStageCompletion(int stageId, bool complete) =>
        Put($"api/stages/{stageId}/toggle", new { complete });

    public void Dispose() => _httpClient.Dispose();

    private void Post<T>(string path, T payload)
    {
        Send(() => _httpClient.PostAsJsonAsync(path, payload, _jsonOptions));
        RaiseChanged();
    }

    private void Put<T>(string path, T payload)
    {
        Send(() => _httpClient.PutAsJsonAsync(path, payload, _jsonOptions));
        RaiseChanged();
    }

    private void Delete(string path)
    {
        Send(() => _httpClient.DeleteAsync(path));
        RaiseChanged();
    }

    private T? Send<T>(Func<Task<T?>> action)
    {
        try
        {
            return action().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"无法连接云端服务：{ex.Message}", ex);
        }
    }

    private void Send(Func<Task<HttpResponseMessage>> action)
    {
        try
        {
            using var response = action().GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"云端保存失败：{ex.Message}", ex);
        }
    }

    private void RaiseChanged()
    {
        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string ToDateOnlyText(DateTime value) => value.Date.ToString("yyyy-MM-dd");

    private static string ToDateTimeText(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss");
}
