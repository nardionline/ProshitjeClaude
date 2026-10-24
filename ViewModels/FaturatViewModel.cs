using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;

namespace MarketPrenga.ViewModels;

public partial class FaturatViewModel : ObservableObject
{
    readonly FaturatRepo _repo = new();

    public ObservableCollection<FaturaRow> Items { get; } = new();

    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today;
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private FaturaRow? _selected;
    [ObservableProperty] private string _summary = "";

    public FaturatViewModel() => Refresh();

    [RelayCommand]
    void Refresh()
    {
        try
        {
            Items.Clear();
            var rows = _repo.List(DateFrom ?? DateTime.Today, DateTo ?? DateTime.Today);
            foreach (var r in rows) Items.Add(r);
            var ok = rows.Where(r => !r.Anulluar).ToList();
            Summary = $"{ok.Count} fatura aktive   ·   Total: {ok.Sum(r => r.Total):N2}" +
                      (rows.Count != ok.Count ? $"   ·   {rows.Count - ok.Count} të anuluara" : "");
        }
        catch (Exception ex) { Summary = ex.Message; }
    }

    [RelayCommand]
    void Cancel()
    {
        if (Selected == null) return;
        if (Selected.Anulluar)
        {
            MessageBox.Show("Kjo faturë është anuluar tashmë.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var msg = $"Anulo faturën Nr {Selected.Nr} me total {Selected.Total:N2}?\n\nStoku do të kthehet." +
                  (Selected.Fiskale ? "\n\nKUJDES: fatura është printuar në kasë fiskale; anulimi fiskal bëhet veçmas në aparat." : "");
        if (MessageBox.Show(msg, "Konfirmo anulimin", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        try { _repo.Cancel(Selected.Id); Refresh(); }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u anulua:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
