using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using demo_hard.Model;

namespace demo_hard
{
    public partial class History : Window
    {
        private readonly List<LoginRecord> _all = new();
        private readonly ObservableCollection<LoginRecord> _view = new();

        private readonly ObservableCollection<DailyStat> _byDate = new();

        public History()
        {
            InitializeComponent();
        
            HistoryList.ItemsSource = _view;
        
            ByDateList.ItemsSource = _byDate;
        
            LoadData();
        
            UserFilter.SelectionChanged += (_, __) => ApplyFilters();
            SortFilter.SelectionChanged += (_, __) => ApplyFilters();
            StatusFilter.SelectionChanged += (_, __) => ApplyFilters();
            ResetBtn.Click += (_, __) =>
            {
                UserFilter.SelectedIndex = 0;
                SortFilter.SelectedIndex = 0;
                StatusFilter.SelectedIndex = 0;
                ApplyFilters();
            };
        }
        
        private void LoadData()
        {
            using var db = new User15Context();
            var list = db.Employees
                         .AsEnumerable()
                         .OrderByDescending(e => e.LastEnter)
                         .Select(e => new LoginRecord
                         {
                             Login = e.Login,
                             LastEnter = e.LastEnter,
                             EnterStatus = e.EnterStatus ?? "Неизвестно"
                         })
                         .ToList();
        
            _all.AddRange(list);
            foreach (var r in _all)
                _view.Add(r);
        
            var users = _all.Select(r => r.Login)
                            .Distinct()
                            .OrderBy(n => n)
                            .ToList();
            UserFilter.ItemsSource = new List<string> { "Все" }.Concat(users);
            UserFilter.SelectedIndex = 0;
        
            SortFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
        
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            var q = _all.AsEnumerable();
        
            if (UserFilter.SelectedItem is string u && u != "Все")
                q = q.Where(r => r.Login == u);
        
            if (StatusFilter.SelectedIndex == 1)
                q = q.Where(r => r.EnterStatus.Contains("Успешно"));
            else if (StatusFilter.SelectedIndex == 2)
                q = q.Where(r => r.EnterStatus.Contains("Неуспешно"));
        
            q = SortFilter.SelectedIndex == 0
                ? q.OrderByDescending(r => r.LastEnter)
                : q.OrderBy(r => r.LastEnter);
        
            _view.Clear();
            foreach (var r in q)
                _view.Add(r);
        
            var successCount = q.Count(r => r.EnterStatus.Contains("Успешно"));
            var failCount = q.Count(r => r.EnterStatus.Contains("Неуспешно"));
            SummaryText.Text = $"Успешных попыток: {successCount} | Неуспешных попыток: {failCount}";
        
            var byDate = q.GroupBy(r => r.LastEnter?.Date)
                          .OrderByDescending(g => g.Key)
                          .Select(g => new DailyStat
                          {
                              Date = g.Key?.ToString("dd.MM.yyyy") ?? "—",
                              Count = g.Count()
                          })
                          .ToList();
            ByDateList.ItemsSource = byDate;
        }
    }

    public class LoginRecord
    {
        public string Login { get; set; }
        public DateTime? LastEnter { get; set; }
        public string EnterStatus { get; set; }
    }

    public class DailyStat
    {
        public string Date { get; set; }
        public int Count { get; set; }
    }
}
