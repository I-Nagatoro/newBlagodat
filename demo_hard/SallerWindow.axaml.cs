using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using demo_hard.Model;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.EntityFrameworkCore;

namespace demo_hard
{
    public partial class SallerWindow : Window, INotifyPropertyChanged
    {
        private readonly User15Context _context = new();

        public ObservableCollection<Client> ClientList { get; } = new();
        public List<Client> ClientsList { get; set; } = new();

        public ObservableCollection<Service> ServiceList { get; } = new();
        public ObservableCollection<ServiceWithTime> BasketCollection { get; } = new();

        public class ServiceWithTime : Service
        {
            public int TimeInMinutes { get; set; } = 30;
        }

        private Client _chosenClient;
        public Client ChosenClient
        {
            get => _chosenClient;
            set { _chosenClient = value; OnPropertyChanged(nameof(ChosenClient)); }
        }

        private Service _chosenService;
        public Service ChosenService
        {
            get => _chosenService;
            set { _chosenService = value; OnPropertyChanged(nameof(ChosenService)); }
        }

        private int _minutesSelected = 30;
        public int MinutesSelected
        {
            get => _minutesSelected;
            set { _minutesSelected = value; OnPropertyChanged(nameof(MinutesSelected)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public SallerWindow()
        {
            InitializeComponent();
            DataContext = this;

            OrderNumberField.Text = "Будет сгенерирован при оформлении";

            _ = LoadInitialDataAsync();
            ReloadClients();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                await _context.Clients.LoadAsync();
                await _context.Services.LoadAsync();

                ClientList.Clear();
                ServiceList.Clear();

                foreach (var c in _context.Clients.Local)
                    ClientList.Add(c);

                foreach (var s in _context.Services.Local)
                    ServiceList.Add(s);
            }
            catch (Exception ex)
            {
                InfoText.Text = $"Ошибка загрузки данных: {ex.Message}";
            }
        }

        private void AddClientClick(object? sender, RoutedEventArgs e)
        {
            var addClientWindow = new AddClient(this);
            addClientWindow.ShowDialog(this);
            ReloadClients();
        }
        
        public void ReloadClients()
        {
            using var context = new User15Context();
            var clients = context.Clients.ToList();

            ClientComboBox.ItemsSource = clients;
        }
        
        private void AddServiceClick(object? sender, RoutedEventArgs e)
        {
            if (ChosenService == null) return;

            if (BasketCollection.Any(s => s.ServiceId == ChosenService.ServiceId))
            {
                InfoText.Text = "Эта услуга уже добавлена.";
                return;
            }

            BasketCollection.Add(new ServiceWithTime
            {
                ServiceId     = ChosenService.ServiceId,
                ServiceName   = ChosenService.ServiceName,
                CostPerHour   = ChosenService.CostPerHour,
                TimeInMinutes = MinutesSelected
            });
        }

        private void RemoveServiceClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is ServiceWithTime svc)
                BasketCollection.Remove(svc);
        }

        private async void CompleteOrderClick(object? sender, RoutedEventArgs e)
        {
            InfoText.Text = string.Empty;

            if (ChosenClient == null)
            {
                InfoText.Text = "Выберите клиента.";
                return;
            }
            if (!BasketCollection.Any())
            {
                InfoText.Text = "Добавьте хотя бы одну услугу.";
                return;
            }

            try
            {
                var code = GenerateOrderCode();
                OrderNumberField.Text = code;

                var order = new Order
                {
                    ClientId  = ChosenClient.ClientId,
                    StartDate = DateOnly.FromDateTime(DateTime.Now),
                    Time      = TimeOnly.FromDateTime(DateTime.Now),
                    Status    = "Оформлен",
                    OrderCode = code
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var svc in BasketCollection)
                {
                    _context.OrderServices.Add(new OrderService
                    {
                        OrderId   = order.OrderId,
                        ServiceId = svc.ServiceId,
                        RentTime  = svc.TimeInMinutes
                    });
                }
                await _context.SaveChangesAsync();

                CreateReceipt(order);
                ShowBarcode(order.OrderId);
                Close();
            }
            catch (Exception ex)
            {
                InfoText.Text = $"Ошибка при оформлении: {ex.Message}";
            }
        }

        private static string GenerateOrderCode() =>
            $"{Random.Shared.Next(100000, 999999)}/{DateTime.Now:yyyyMMdd}";

        private void CreateReceipt(Model.Order order)
        {
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string outputFile = Path.Combine(documentsPath, $"Чек_{order.OrderCode?.Replace("/", "_")}.pdf");
            
            string fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "arialmt.ttf");
            BaseFont baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            var titleFont = new Font(baseFont, 18, Font.BOLD);
            var textFont = new Font(baseFont, 12);
        
            using (var doc = new Document(PageSize.A4, 40, 40, 40, 40))
            {
                PdfWriter.GetInstance(doc, new FileStream(outputFile, FileMode.Create));
                doc.Open();
        
                var title = new Paragraph("Чек заказа", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                doc.Add(title);
        
                PdfPTable details = new PdfPTable(2) { WidthPercentage = 100 };
                AddRow(details, "Номер заказа:", order.OrderCode ?? "—", textFont);
                AddRow(details, "Дата:", $"{order.StartDate:dd.MM.yyyy}", textFont);
                AddRow(details, "Время:", $"{order.Time:HH:mm}", textFont);
                AddRow(details, "Клиент:", ChosenClient?.Fio ?? "—", textFont);
                doc.Add(details);
        
                PdfPTable servicesTable = new PdfPTable(3) { WidthPercentage = 100 };
                servicesTable.AddCell(new PdfPCell(new Phrase("Услуга", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                servicesTable.AddCell(new PdfPCell(new Phrase("Цена/ч", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
                servicesTable.AddCell(new PdfPCell(new Phrase("Минуты", textFont)) { BackgroundColor = BaseColor.LIGHT_GRAY });
        
                if (BasketCollection.Any())
                {
                    foreach (var item in BasketCollection)
                    {
                        servicesTable.AddCell(new Phrase(item.ServiceName ?? "—", textFont));
                        servicesTable.AddCell(new Phrase($"{(item.CostPerHour ?? 0m):N2} ₽", textFont));
                        servicesTable.AddCell(new Phrase(item.TimeInMinutes.ToString(), textFont));
                    }
                }
                else
                {
                    var noServicesCell = new PdfPCell(new Phrase("Нет услуг", textFont))
                    {
                        Colspan = 3,
                        HorizontalAlignment = Element.ALIGN_CENTER
                    };
                    servicesTable.AddCell(noServicesCell);
                }
        
                doc.Add(servicesTable);
        
                decimal total = BasketCollection.Any() 
                    ? BasketCollection.Sum(s => (s.CostPerHour ?? 0m) * (s.TimeInMinutes / 60m)) 
                    : 0m;
                    
                doc.Add(new Paragraph($"Итого: {total:N2} ₽", textFont) 
                { 
                    Alignment = Element.ALIGN_RIGHT, 
                    SpacingBefore = 20 
                });
        
                doc.Close();
            }
        
            Process.Start(new ProcessStartInfo(outputFile) { UseShellExecute = true });
        }

        private void AddRow(PdfPTable table, string label, string value, Font font)
        {
            table.AddCell(new PdfPCell(new Phrase(label, font)) { Border = Rectangle.NO_BORDER });
            table.AddCell(new PdfPCell(new Phrase(value, font)) { Border = Rectangle.NO_BORDER });
        }

        private void ShowBarcode(int orderId)
        {
            int maxTime = BasketCollection.Max(s => s.TimeInMinutes);
            new OrderBarcode(orderId, maxTime).Show();
        }
    }
}
