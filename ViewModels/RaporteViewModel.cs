using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;

namespace MarketPrenga.ViewModels;

public partial class RaporteViewModel : ObservableObject
{
    readonly ReportRepo _repo = new();

    public ObservableCollection<DayTotal> Daily { get; } = new();
    public ObservableCollection<TopItem> Top { get; } = new();
    public ObservableCollection<UserTotal> Users { get; } = new();

    [ObservableProperty] private DateTime? _dateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private int _count;
    [ObservableProperty] private decimal _average;
    [ObservableProperty] private string _error = "";

    public RaporteViewModel() => Refresh();

    [RelayCommand]
    void Refresh()
    {
        try
        {
            Error = "";
            var from = DateFrom ?? DateTime.Today;
            var to = DateTo ?? DateTime.Today;

            Daily.Clear(); Top.Clear(); Users.Clear();
            foreach (var d in _repo.Daily(from, to)) Daily.Add(d);
            foreach (var t in _repo.Top(from, to, 20)) Top.Add(t);
            foreach (var u in _repo.ByUser(from, to)) Users.Add(u);

            Total = Daily.Sum(d => d.Total);
            Count = Daily.Sum(d => d.Count);
            Average = Count > 0 ? Total / Count : 0m;
        }
        catch (Exception ex) { Error = ex.Message; }
    }

    [RelayCommand]
    void ExportCsv()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv", FileName = $"xhiro_{DateTime.Today:yyyyMMdd}.csv"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var ci = CultureInfo.CurrentCulture;
            var sb = new StringBuilder();
            sb.AppendLine("Data;Nr faturave;Totali");
            foreach (var d in Daily) sb.AppendLine($"{d.Date:dd.MM.yyyy};{d.Count};{d.Total.ToString("0.00", ci)}");
            sb.AppendLine();
            sb.AppendLine("Artikulli;Sasia;Totali");
            foreach (var t in Top) sb.AppendLine($"{t.Name.Replace(';', ',')};{t.Qty.ToString("0.###", ci)};{t.Total.ToString("0.00", ci)}");
            File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
            MessageBox.Show("U eksportua.", "Në rregull", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
