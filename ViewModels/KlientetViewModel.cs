using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;

namespace MarketPrenga.ViewModels;

public partial class KlientetViewModel : ObservableObject
{
    readonly KlientRepo _repo = new();

    public ObservableCollection<Klient> Items { get; } = new();
    public string[] Rishitesit { get; } = { "Klient", "R1", "R2", "R3" };

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Klient? _selected;
    [ObservableProperty] private string _status = "";
    [ObservableProperty] private string _payAmountText = "";

    public KlientetViewModel() => Load();

    partial void OnSearchTextChanged(string value) => Load();

    void Load()
    {
        try
        {
            Items.Clear();
            foreach (var k in _repo.Search(SearchText)) Items.Add(k);
            Status = $"{Items.Count} klientë";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    void New()
    {
        var k = new Klient();
        Items.Insert(0, k);
        Selected = k;
    }

    [RelayCommand]
    void Save()
    {
        if (Selected == null) return;
        if (string.IsNullOrWhiteSpace(Selected.Emri))
        {
            MessageBox.Show("Emri është i detyrueshëm.", "Kujdes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try { _repo.Save(Selected); Load(); Status = "U ruajt."; }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u ruajt:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    void Delete()
    {
        if (Selected == null) return;
        if (MessageBox.Show($"Fshi klientin '{Selected.Emri}'?", "Konfirmo", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { if (Selected.Id != 0) _repo.Delete(Selected.Id); Load(); }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u fshi:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    void PayDebt()
    {
        if (Selected == null || Selected.Id == 0)
        {
            MessageBox.Show("Ruaje klientin fillimisht.", "Kujdes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var txt = (PayAmountText ?? "").Trim().Replace(',', '.');
        if (!decimal.TryParse(txt, System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            MessageBox.Show("Shkruaj një shumë të vlefshme.", "Kujdes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var neu = new DebtRepo().PayClient(Selected.Id, amount);
            var id = Selected.Id;
            PayAmountText = "";
            Load();
            Selected = Items.FirstOrDefault(k => k.Id == id);
            Status = $"Pagesa u regjistrua. Borxhi i ri: {neu:N2}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u regjistrua:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
