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
            var dialog = CreateDialog(caption);
            var result = MessageBoxResult.None;
            var stack = CreateContentStack(dialog);

            stack.Children.Add(CreateTitle(string.IsNullOrWhiteSpace(caption) ? "提示" : caption));
            stack.Children.Add(CreateMessage(messageBoxText));

            var buttons = CreateButtonPanel();
            stack.Children.Add(buttons);

            foreach (var option in BuildButtons(button))
            {
                buttons.Children.Add(CreateDialogButton(option.Text, option.IsPrimary, () =>
                {
                    result = option.Result;
                    dialog.DialogResult = true;
                    dialog.Close();
                }));
            }

            dialog.ShowDialog();
            return result == MessageBoxResult.None ? MessageBoxResult.Cancel : result;
        });
    }

    public static string? PromptPassword(string messageBoxText, string caption = "请输入确认密码")
    {
        if (Application.Current?.Dispatcher is null)
        {
            return null;
        }

        return Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = CreateDialog(caption);
            string? password = null;
            var stack = CreateContentStack(dialog);

            stack.Children.Add(CreateTitle(caption));
            stack.Children.Add(CreateMessage(messageBoxText, bottomMargin: 14));

            var input = new PasswordBox
            {
                Height = 42,
                FontSize = 16,
                Padding = new Thickness(12, 0, 12, 0),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Color.FromArgb(185, 244, 247, 251))
            };
            stack.Children.Add(input);

            var buttons = CreateButtonPanel(new Thickness(0, 22, 0, 0));
            buttons.Children.Add(CreateDialogButton("取消", false, () => dialog.Close()));
            buttons.Children.Add(CreateDialogButton("确认", true, () =>
            {
                password = input.Password;
                dialog.DialogResult = true;
                dialog.Close();
            }));
            stack.Children.Add(buttons);

            dialog.Loaded += (_, _) => input.Focus();
            dialog.ShowDialog();
            return password;
        });
    }

    private static Window CreateDialog(string caption) =>
        new()
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

    private static StackPanel CreateContentStack(Window dialog)
    {
        var root = new Border
        {
            CornerRadius = new CornerRadius(28),
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
        root.Child = stack;
        dialog.Content = root;
        return stack;
    }

    private static TextBlock CreateTitle(string text) =>
        new()
        {
            Text = text,
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(22, 26, 34))
        };

    private static TextBlock CreateMessage(string text, double bottomMargin = 22) =>
        new()
        {
            Text = text,
            Margin = new Thickness(0, 14, 0, bottomMargin),
            FontSize = 15,
            LineHeight = 23,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(72, 82, 98))
        };

    private static StackPanel CreateButtonPanel(Thickness? margin = null) =>
        new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = margin ?? new Thickness(0)
        };

    private static Button CreateDialogButton(string text, bool isPrimary, Action action)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 86,
            Height = 38,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(16, 0, 16, 0),
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = isPrimary
                ? new LinearGradientBrush(Color.FromRgb(36, 123, 255), Color.FromRgb(94, 161, 255), 0)
                : new SolidColorBrush(Color.FromArgb(175, 238, 242, 248)),
            Foreground = isPrimary ? Brushes.White : new SolidColorBrush(Color.FromRgb(34, 42, 56)),
            FontWeight = FontWeights.SemiBold
        };
        button.Click += (_, _) => action();
        return button;
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
