using Avalonia.Controls;
using Avalonia.Interactivity;
using demo_hard.Model;
using System;
using System.Linq;

namespace demo_hard;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    private void TogglePasswordVisibility(object? sender, RoutedEventArgs e)
    {
        var hidden = PasswordBox.PasswordChar == '*';
        PasswordBox.PasswordChar = hidden ? '\0' : '*';
    }

    private void AuthorizeButton(object? sender, RoutedEventArgs e)
    {
        using var db = new User15Context();

        var login = LoginBox.Text;
        var password = PasswordBox.Text;

        var employee = db.Employees.FirstOrDefault(emp => emp.Login == login && emp.Password == password);

        if (employee is null)
        {
            ErrorBlock.Text = "Неверный пароль";
            return;
        }

        var dashboard = new UserDashboardWindow(employee)
        {
            DataContext = employee
        };

        dashboard.ShowDialog(this);
    }
}