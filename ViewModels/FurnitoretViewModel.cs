using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;

namespace MarketPrenga.ViewModels;

public partial class FurnitoretViewModel : ObservableObject
{
    readonly FurnitorRepo _repo = new();

    public ObservableCollection<Furnitor> Items { get; } = new();

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Furnitor? _selected;
    [ObservableProperty] private string _status = "";

    public FurnitoretViewModel() => Load();

    partial void OnSearchTextChanged(string value) => Load();

    void Load()
    {
        try
        {
            Items.Clear();
            foreach (var f in _repo.Search(SearchText)) Items.Add(f);
            Status = $"{Items.Count} furnitorë";
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    void New()
    {
        var f = new Furnitor();
        Items.Insert(0, f);
        Selected = f;
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
        if (MessageBox.Show($"Fshi furnitorin '{Selected.Emri}'?", "Konfirmo", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { if (Selected.Id != 0) _repo.Delete(Selected.Id); Load(); }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u fshi:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
