using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using demo_hard.Model;
using System;
using System.IO;
using System.Threading.Tasks;

namespace demo_hard;

public partial class UserDashboardWindow : Window
{
    private readonly TimeSpan sessionLimit = TimeSpan.FromMinutes(10);
    private readonly TimeSpan warnThreshold = TimeSpan.FromMinutes(5);

    private DateTime sessionStart;
    private bool warningIssued = false;
    private readonly Employee currentUser;

    public UserDashboardWindow() => InitializeComponent();

    public UserDashboardWindow(Employee user) : this()
    {
        currentUser = user;
        sessionStart = DateTime.Now;

        DisplayUserInfo();
        _ = MonitorSessionAsync();
    }

    private void DisplayUserInfo()
    {
        FioTextBlock.Text = currentUser.Fio;
        RoleTextBlock.Text = currentUser.Role == 3 ? "Администратор" : "Сотрудник";

        CreateOrderButton.IsVisible = currentUser.Role != 3;
        HistoryButton.IsVisible = currentUser.Role == 3;

        SetUserPhoto();
    }

    private void SetUserPhoto()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(currentUser.Photo))
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, currentUser.Photo);

                if (File.Exists(fullPath))
                {
                    UserImage.Source = new Bitmap(fullPath);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка изображения: {ex.Message}");
        }

        UserImage.Source = null;
    }

    private async Task MonitorSessionAsync()
    {
        while (true)
        {
            var elapsed = DateTime.Now - sessionStart;
            var remaining = sessionLimit - elapsed;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SessionTimerText.Text = remaining > TimeSpan.Zero
                    ? $"Осталось: {remaining:mm\\:ss}"
                    : "Время вышло";

                if (!warningIssued && remaining <= warnThreshold && remaining > TimeSpan.Zero)
                {
                    warningIssued = true;
                    SessionWarningText.Text = "Внимание: до окончания сеанса ≤5 мин!";
                }
            });

            if (remaining <= TimeSpan.Zero)
            {
                Close();
                break;
            }

            await Task.Delay(1000);
        }
    }

    private void OnCreateOrderClick(object? sender, RoutedEventArgs e) => new SallerWindow().ShowDialog(this);

    private void OnViewHistoryClick(object? sender, RoutedEventArgs e) => new History().ShowDialog(this);

    private void OnLogoutClick(object? sender, RoutedEventArgs e) => Close();
}
