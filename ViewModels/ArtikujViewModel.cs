using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;

namespace MarketPrenga.ViewModels;

public partial class ArtikujViewModel : ObservableObject
{
    readonly ArtikujRepo _repo = new();

    public ObservableCollection<Artikull> Items { get; } = new();
    public List<Kategori> Kategorite { get; }

    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Artikull? _selected;
    [ObservableProperty] private string _status = "";

    public ArtikujViewModel()
    {
        Kategorite = new KategoriRepo().All();
        Load();
    }

    partial void OnSearchTextChanged(string value) => Load();

    void Load(int? reselectId = null)
    {
        try
        {
            Items.Clear();
            foreach (var a in _repo.Search(SearchText)) Items.Add(a);
            Status = $"{Items.Count} artikuj";
            if (reselectId != null) Selected = Items.FirstOrDefault(x => x.Id == reselectId);
        }
        catch (Exception ex) { Status = ex.Message; }
    }

    [RelayCommand]
    void New()
    {
        var a = new Artikull { IdCat = Kategorite.FirstOrDefault()?.Id ?? 0 };
        Items.Insert(0, a);
        Selected = a;
    }

    [RelayCommand]
    void Save()
    {
        if (Selected == null) return;
        if (string.IsNullOrWhiteSpace(Selected.Emri))
        {
            MessageBox.Show("Emri i artikullit është i detyrueshëm.", "Kujdes", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            _repo.Save(Selected);
            var bc = Selected.Barcodi;
            Load();
            Selected = Items.FirstOrDefault(x => x.Barcodi == bc && bc != "") ?? Selected;
            Status = "U ruajt.";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u ruajt:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    void Delete()
    {
        if (Selected == null) return;
        if (MessageBox.Show($"Fshi artikullin '{Selected.Emri}'?", "Konfirmo", MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            if (Selected.Id != 0) _repo.Delete(Selected.Id);
            Load();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Nuk u fshi (ndoshta ka lëvizje në fatura):\n" + ex.Message, "Gabim",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
