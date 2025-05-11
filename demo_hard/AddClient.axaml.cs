using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using demo_hard.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace demo_hard;

public partial class AddClient : Window
{
    private readonly SallerWindow? _ownerWindow;
    public AddClient(SallerWindow ownerWindow)
    {
        InitializeComponent();
        _ownerWindow = ownerWindow;
    }

    private void BackOnOrder(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void AddClient_OnClick(object? sender, RoutedEventArgs e)
    {
        using var context = new User15Context();
        if (!ValidateInputs(out int code, out DateOnly birthdayDate, out string email, out string password))
            return;

        if (context.Clients.Any(c => c.ClientCode == code))
        {
            ShowMessage("Клиент с таким кодом уже существует!", false);
            return;
        }

        var client = new Client
        {
            Fio = FioBox.Text.Trim(),
            ClientCode = code,
            Passport = PassportBox.Text.Trim(),
            Birthday = birthdayDate,
            Address = AddressBox.Text.Trim(),
            Email = email,
            Password = password,
            Role = 1
        };

        context.Clients.Add(client);
        context.SaveChanges();

        _ownerWindow.ReloadClients();
        this.Close();
    }

    private bool ValidateInputs(out int code, out DateOnly birthday, out string email, out string password)
    {
        code = 0;
        birthday = default;
        email = EmailBox.Text?.Trim() ?? string.Empty;
        password = PasswordBox.Text?.Trim() ?? string.Empty;

        if (new[] { CodeBox.Text, FioBox.Text, AddressBox.Text, BirthdayBox.Text, PassportBox.Text, email, password }
            .Any(string.IsNullOrWhiteSpace))
        {
            ShowMessage("Пожалуйста, заполните все поля!", false);
            return false;
        }

        if (!Regex.IsMatch(CodeBox.Text, @"^\d{8}$"))
        {
            ShowMessage("Код клиента должен содержать ровно 8 цифр.", false);
            return false;
        }

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ShowMessage("Введите корректный адрес электронной почты.", false);
            return false;
        }

        if (!DateOnly.TryParse(BirthdayBox.Text, out birthday))
        {
            ShowMessage("Некорректный формат даты рождения.", false);
            return false;
        }

        if (PassportBox.Text.Length != 10 || !PassportBox.Text.All(char.IsDigit))
        {
            ShowMessage("Паспорт должен содержать ровно 10 цифр", false);
            return false;
        }

        if (password.Length < 6)
        {
            ShowMessage("Пароль должен содержать минимум 6 символов.", false);
            return false;
        }

        return int.TryParse(CodeBox.Text, out code);
    }

    private void ShowMessage(string text, bool isSuccess)
    {
        MessageText.Text = text;
        MessageBorder.Background = isSuccess ? Brushes.LightGreen : Brushes.LightCoral;
        MessageBorder.IsVisible = true;
    }

    private void ClearFields()
    {
        FioBox.Clear();
        CodeBox.Clear();
        PassportBox.Clear();
        BirthdayBox.Clear();
        AddressBox.Clear();
        EmailBox.Clear();
        PasswordBox.Clear();
        MessageBorder.IsVisible = false;
    }
}