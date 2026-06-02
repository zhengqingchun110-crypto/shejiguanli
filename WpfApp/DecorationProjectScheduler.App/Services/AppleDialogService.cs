using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DecorationProjectScheduler.App.Services;

public static class AppleDialogService
{
    public static MessageBoxResult Show(string messageBoxText, string caption = "", MessageBoxButton button = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        if (Application.Current?.Dispatcher is null)
        {
            return System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);
        }

        return Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new Window
            {
                Title = caption,
                Width = 430,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                    ?? Application.Current.MainWindow
            };

            var result = MessageBoxResult.None;
            var root = new Border
            {
                CornerRadius = new CornerRadius(28),
                Padding = new Thickness(1),
                Background = new LinearGradientBrush(
                    Color.FromArgb(190, 255, 255, 255),
                    Color.FromArgb(150, 230, 238, 248),
                    135)
            };

            var panel = new Border
            {
                CornerRadius = new CornerRadius(27),
                Padding = new Thickness(26),
                Background = new LinearGradientBrush(
                    Color.FromArgb(238, 255, 255, 255),
                    Color.FromArgb(218, 241, 245, 251),
                    120),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 34,
                    ShadowDepth = 14,
                    Opacity = 0.22,
                    Color = Color.FromRgb(35, 45, 65)
                }
            };

            var stack = new StackPanel();
            panel.Child = stack;
            root.Child = panel;

            var title = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(caption) ? "提示" : caption,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(22, 26, 34))
            };
            stack.Children.Add(title);

            stack.Children.Add(new TextBlock
            {
                Text = messageBoxText,
                Margin = new Thickness(0, 14, 0, 22),
                FontSize = 15,
                LineHeight = 23,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(72, 82, 98))
            });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            stack.Children.Add(buttons);

            foreach (var option in BuildButtons(button))
            {
                var dialogButton = new Button
                {
                    Content = option.Text,
                    MinWidth = 86,
                    Height = 38,
                    Margin = new Thickness(8, 0, 0, 0),
                    Padding = new Thickness(16, 0, 16, 0),
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Background = option.IsPrimary
                        ? new LinearGradientBrush(Color.FromRgb(36, 123, 255), Color.FromRgb(94, 161, 255), 0)
                        : new SolidColorBrush(Color.FromArgb(175, 238, 242, 248)),
                    Foreground = option.IsPrimary ? Brushes.White : new SolidColorBrush(Color.FromRgb(34, 42, 56)),
                    FontWeight = FontWeights.SemiBold
                };
                dialogButton.Click += (_, _) =>
                {
                    result = option.Result;
                    dialog.DialogResult = true;
                    dialog.Close();
                };
                buttons.Children.Add(dialogButton);
            }

            dialog.Content = root;
            dialog.ShowDialog();
            return result == MessageBoxResult.None ? MessageBoxResult.Cancel : result;
        });
    }

    private static IEnumerable<DialogOption> BuildButtons(MessageBoxButton button) =>
        button switch
        {
            MessageBoxButton.OKCancel =>
            [
                new("取消", MessageBoxResult.Cancel, false),
                new("确定", MessageBoxResult.OK, true)
            ],
            MessageBoxButton.YesNo =>
            [
                new("取消", MessageBoxResult.No, false),
                new("确认", MessageBoxResult.Yes, true)
            ],
            MessageBoxButton.YesNoCancel =>
            [
                new("取消", MessageBoxResult.Cancel, false),
                new("不保存", MessageBoxResult.No, false),
                new("保存", MessageBoxResult.Yes, true)
            ],
            _ =>
            [
                new("知道了", MessageBoxResult.OK, true)
            ]
        };

    private sealed record DialogOption(string Text, MessageBoxResult Result, bool IsPrimary);
}
