using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MarketPrenga.Data;
using MarketPrenga.Models;
using MarketPrenga.Services;

namespace MarketPrenga.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    public string Greeting => $"Mirësevjen, {Session.User?.Emri}";
    public string Today => DateTime.Today.ToString("dddd, dd MMMM yyyy");

    [ObservableProperty] private decimal _todayTotal;
    [ObservableProperty] private int _todayCount;
    [ObservableProperty] private decimal _monthTotal;
    [ObservableProperty] private decimal _debtTotal;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private string _error = "";

    public ObservableCollection<DayBar> Bars { get; } = new();
    public ObservableCollection<StockRow> LowStock { get; } = new();
    public ObservableCollection<TopItem> TopToday { get; } = new();

    public DashboardViewModel() => Load();

    void Load()
    {
        try
        {
            var rr = new ReportRepo();
            var today = DateTime.Today;

            var week = rr.Daily(today.AddDays(-6), today).ToDictionary(d => d.Date, d => d);
            var vals = Enumerable.Range(0, 7).Select(i => today.AddDays(-6 + i))
                .Select(d => (d, v: week.TryGetValue(d, out var x) ? x.Total : 0m)).ToList();
            var max = vals.Max(x => x.v);
            foreach (var (d, v) in vals)
                Bars.Add(new DayBar(d.ToString("ddd dd.MM"), v, max > 0 ? Math.Max(3, (double)(v / max) * 150) : 3));

            TodayTotal = week.TryGetValue(today, out var t) ? t.Total : 0m;
            TodayCount = week.TryGetValue(today, out var t2) ? t2.Count : 0;
            MonthTotal = rr.Daily(new DateTime(today.Year, today.Month, 1), today).Sum(d => d.Total);
            DebtTotal = rr.DebtTotal();

            foreach (var i in rr.Top(today, today, 6)) TopToday.Add(i);

            var low = rr.LowStock(AppConfig.Current.LowStockThreshold);
            LowStockCount = low.Count;
            foreach (var s in low.Take(15)) LowStock.Add(s);
        }
        catch (Exception ex) { Error = "Gabim në leximin e statistikave: " + ex.Message; }
    }
}
