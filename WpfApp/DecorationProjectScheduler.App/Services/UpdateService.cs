using System.Diagnostics;
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
