using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;

namespace DecorationProjectScheduler.App.Services;

public sealed class UpdateService
{
    private readonly string? _apiBaseUrl;

    public UpdateService(string? apiBaseUrl)
    {
        _apiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? null : apiBaseUrl.TrimEnd('/');
    }

    public Version CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0, 0);

    public bool CanCheckOnline => !string.IsNullOrWhiteSpace(_apiBaseUrl);

    public async Task<UpdateInfo?> CheckLatestAsync()
    {
        if (!CanCheckOnline)
        {
            return null;
        }

        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(_apiBaseUrl! + "/"),
            Timeout = TimeSpan.FromSeconds(8)
        };

        var manifest = await httpClient.GetFromJsonAsync<UpdateManifest>("api/update/latest");
        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
        {
            return null;
        }

        return Version.TryParse(manifest.Version, out var latestVersion)
            ? new UpdateInfo(CurrentVersion, latestVersion, manifest.DownloadUrl, manifest.Notes)
            : null;
    }

    public async Task StartAutoUpdateAsync(UpdateInfo updateInfo)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
        {
            throw new InvalidOperationException("没有获取到更新包下载地址。");
        }

        var updaterPath = Path.Combine(AppContext.BaseDirectory, "DecorationProjectScheduler.Updater.exe");
        if (!File.Exists(updaterPath))
        {
            throw new InvalidOperationException("没有找到自动更新程序，请使用正式发布版更新。");
        }

        var packageDirectory = Path.Combine(
            Path.GetTempPath(),
            "DecorationProjectScheduler",
            "Updates",
            updateInfo.LatestVersion.ToString());
        Directory.CreateDirectory(packageDirectory);

        var packagePath = Path.Combine(packageDirectory, "DesignScheduler-CloudClient.zip");
        using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
        using (var response = await httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync();
            await using var target = File.Create(packagePath);
            await source.CopyToAsync(target);
        }

        var currentProcess = Process.GetCurrentProcess();
        var executablePath = Path.Combine(AppContext.BaseDirectory, "DecorationProjectScheduler.App.exe");

        var startInfo = new ProcessStartInfo(updaterPath)
        {
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add("--pid");
        startInfo.ArgumentList.Add(currentProcess.Id.ToString());
        startInfo.ArgumentList.Add("--package");
        startInfo.ArgumentList.Add(packagePath);
        startInfo.ArgumentList.Add("--install-dir");
        startInfo.ArgumentList.Add(AppContext.BaseDirectory);
        startInfo.ArgumentList.Add("--exe");
        startInfo.ArgumentList.Add(executablePath);

        Process.Start(startInfo);
    }

    public void OpenDownload(UpdateInfo updateInfo)
    {
        if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(updateInfo.DownloadUrl)
        {
            UseShellExecute = true
        });
    }

    private sealed class UpdateManifest
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}

public sealed record UpdateInfo(Version CurrentVersion, Version LatestVersion, string DownloadUrl, string Notes)
{
    public bool HasUpdate => LatestVersion > CurrentVersion;
}
