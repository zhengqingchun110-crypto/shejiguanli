using System.IO;
using System.Windows;
using DecorationProjectScheduler.App.Database;
using DecorationProjectScheduler.App.Repositories;
using DecorationProjectScheduler.App.Services;
using DecorationProjectScheduler.App.ViewModels;

namespace DecorationProjectScheduler.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        InstallPendingUpdater();

        var appRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DecorationProjectScheduler");
        var databasePath = Path.Combine(appRoot, "Database", "scheduler.db");
        var fileRoot = Path.Combine(appRoot, "ProjectFiles");

        ISchedulerRepository repository;
        var apiBaseUrl = Environment.GetEnvironmentVariable("SCHEDULER_API_URL");
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            var localConfigPath = Path.Combine(AppContext.BaseDirectory, "api-url.txt");
            if (File.Exists(localConfigPath))
            {
                apiBaseUrl = File.ReadAllText(localConfigPath).Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            var appDataConfigPath = Path.Combine(appRoot, "api-url.txt");
            if (File.Exists(appDataConfigPath))
            {
                apiBaseUrl = File.ReadAllText(appDataConfigPath).Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            var initializer = new SqliteInitializer(databasePath);
            initializer.Initialize();
            repository = new SchedulerRepository(initializer.ConnectionString);
        }
        else
        {
            repository = new ApiSchedulerRepository(apiBaseUrl);
        }

        var themeService = new ThemeService();
        var fileStorageService = new FileStorageService(fileRoot);
        var updateService = new UpdateService(apiBaseUrl);
        var securitySettingsService = new SecuritySettingsService();

        Resources["FileStorageService"] = fileStorageService;

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(repository, themeService, updateService, securitySettingsService),
            Title = string.IsNullOrWhiteSpace(apiBaseUrl)
                ? "凡响智道 - 本机模式"
                : "凡响智道 - 云端模式"
        };
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static void InstallPendingUpdater()
    {
        var updaterPath = Path.Combine(AppContext.BaseDirectory, "DecorationProjectScheduler.Updater.exe");
        var nextUpdaterPath = Path.Combine(AppContext.BaseDirectory, "DecorationProjectScheduler.Updater.next.exe");
        if (!File.Exists(nextUpdaterPath))
        {
            return;
        }

        try
        {
            File.Copy(nextUpdaterPath, updaterPath, true);
            File.Delete(nextUpdaterPath);
        }
        catch
        {
            // 下次启动再尝试安装新的更新器，避免影响主程序启动。
        }
    }
}
