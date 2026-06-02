using System.Diagnostics;
using System.IO.Compression;

var options = UpdateOptions.Parse(args);
if (options is null)
{
    return 2;
}

try
{
    WaitForProcessExit(options.ProcessId);
    Directory.CreateDirectory(options.InstallDirectory);

    var backupDirectory = Path.Combine(
        Path.GetTempPath(),
        "DecorationProjectScheduler",
        "UpdateBackups",
        DateTime.Now.ToString("yyyyMMdd-HHmmss"));
    Directory.CreateDirectory(backupDirectory);

    BackupInstallDirectory(options.InstallDirectory, backupDirectory);
    ExtractPackage(options.PackagePath, options.InstallDirectory);
    TryDelete(options.PackagePath);

    StartApplication(options.ExecutablePath);
    return 0;
}
catch (Exception ex)
{
    File.WriteAllText(
        Path.Combine(Path.GetTempPath(), "DecorationProjectScheduler-update-error.txt"),
        ex.ToString());
    return 1;
}

static void WaitForProcessExit(int processId)
{
    if (processId <= 0)
    {
        Thread.Sleep(1000);
        return;
    }

    try
    {
        using var process = Process.GetProcessById(processId);
        process.WaitForExit(30000);
    }
    catch
    {
        Thread.Sleep(1000);
    }
}

static void BackupInstallDirectory(string installDirectory, string backupDirectory)
{
    foreach (var file in Directory.EnumerateFiles(installDirectory, "*", SearchOption.AllDirectories))
    {
        if (ShouldSkipFile(file))
        {
            continue;
        }

        var relative = Path.GetRelativePath(installDirectory, file);
        var target = Path.Combine(backupDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(file, target, true);
    }
}

static void ExtractPackage(string packagePath, string installDirectory)
{
    using var archive = ZipFile.OpenRead(packagePath);
    foreach (var entry in archive.Entries)
    {
        if (string.IsNullOrWhiteSpace(entry.Name))
        {
            Directory.CreateDirectory(Path.Combine(installDirectory, entry.FullName));
            continue;
        }

        var destination = Path.GetFullPath(Path.Combine(installDirectory, entry.FullName));
        if (!destination.StartsWith(Path.GetFullPath(installDirectory), StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        if (ShouldPreserveExistingFile(destination) && File.Exists(destination))
        {
            continue;
        }

        if (ShouldSkipFile(destination) && File.Exists(destination))
        {
            continue;
        }

        entry.ExtractToFile(destination, true);
    }
}

static bool ShouldSkipFile(string filePath)
{
    var name = Path.GetFileName(filePath);
    return name.Equals("DecorationProjectScheduler.Updater.exe", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
}

static bool ShouldPreserveExistingFile(string filePath)
{
    var name = Path.GetFileName(filePath);
    return name.Equals("api-url.txt", StringComparison.OrdinalIgnoreCase);
}

static void TryDelete(string path)
{
    try
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
    catch
    {
    }
}

static void StartApplication(string executablePath)
{
    if (!File.Exists(executablePath))
    {
        return;
    }

    Process.Start(new ProcessStartInfo(executablePath)
    {
        UseShellExecute = true,
        WorkingDirectory = Path.GetDirectoryName(executablePath)
    });
}

internal sealed record UpdateOptions(int ProcessId, string PackagePath, string InstallDirectory, string ExecutablePath)
{
    public static UpdateOptions? Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length - 1; i += 2)
        {
            values[args[i]] = args[i + 1];
        }

        if (!values.TryGetValue("--pid", out var pidText)
            || !int.TryParse(pidText, out var processId)
            || !values.TryGetValue("--package", out var packagePath)
            || !values.TryGetValue("--install-dir", out var installDirectory)
            || !values.TryGetValue("--exe", out var executablePath)
            || string.IsNullOrWhiteSpace(packagePath)
            || string.IsNullOrWhiteSpace(installDirectory)
            || string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        return new UpdateOptions(processId, packagePath, installDirectory, executablePath);
    }
}
