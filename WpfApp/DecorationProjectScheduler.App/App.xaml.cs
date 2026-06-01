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

        Resources["FileStorageService"] = fileStorageService;

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(repository, themeService, updateService),
            Title = string.IsNullOrWhiteSpace(apiBaseUrl)
                ? "装饰设计项目与人员排期管理系统 - 本机模式"
                : "装饰设计项目与人员排期管理系统 - 云端模式"
        };
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
