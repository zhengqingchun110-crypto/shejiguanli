using System.Windows;
using DecorationProjectScheduler.App.Models;
using DecorationProjectScheduler.App.ViewModels;

namespace DecorationProjectScheduler.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    private void ProjectTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel viewModel && e.NewValue is ProjectSummary project)
        {
            viewModel.SelectedProjectSummary = project;
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && !viewModel.ConfirmCloseAndSave())
        {
            e.Cancel = true;
        }
    }
}
