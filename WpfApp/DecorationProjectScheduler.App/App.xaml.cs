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

        var initializer = new SqliteInitializer(databasePath);
        initializer.Initialize();

        var repository = new SchedulerRepository(initializer.ConnectionString);
        var themeService = new ThemeService();
        var fileStorageService = new FileStorageService(fileRoot);

        Resources["FileStorageService"] = fileStorageService;

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(repository, themeService)
        };
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
