using System.Windows;
using DecorationProjectScheduler.App.Services;

namespace DecorationProjectScheduler.App.ViewModels;

internal static class MessageBox
{
    public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon) =>
        AppleDialogService.Show(messageBoxText, caption, button, icon);
}
