using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MarketPrenga.Data;
using MarketPrenga.Models;
using MarketPrenga.Services;

namespace MarketPrenga.ViewModels;

public partial class ShitjaViewModel : ObservableObject
{
    readonly ArtikujRepo _art = new();
    readonly FaturaRepo _fat = new();
    readonly AppSettings _settings = new SettingsRepo().Load();

    public ObservableCollection<SaleLine> Lines { get; } = new();
    public ObservableCollection<Artikull> SearchResults { get; } = new();
    public List<Klient> Klientet { get; }
    public string[] Rishitesit { get; } = { "Klient", "R1", "R2", "R3" };

    public event Action? FocusBarcodeRequested;

    [ObservableProperty] private string _barcodeInput = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string _selectedRishites = "Klient";
    [ObservableProperty] private decimal _skonto;
    [ObservableProperty] private Klient? _selectedKlient;
    [ObservableProperty] private string _paguarText = "";
    [ObservableProperty] private string _status = "Gati. Skano barkodin.";
    [ObservableProperty] private Artikull? _selectedSearch;
    [ObservableProperty] private SaleLine? _selectedLine;

    public decimal Total => Lines.Sum(l => l.Total);
    public int LineCount => Lines.Count;
    public bool ShowDebtInfo => _settings.LejonDebi;

    public ShitjaViewModel()
    {
        Klientet = new List<Klient> { new Klient { Id = 0, Emri = "(pa klient)" } };
        try { Klientet.AddRange(new KlientRepo().Search(null)); } catch { }
        _selectedKlient = Klientet[0];

        Lines.CollectionChanged += OnLinesChanged;
    }

    void OnLinesChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null) foreach (SaleLine l in e.NewItems) l.PropertyChanged += OnLineChanged;
        if (e.OldItems != null) foreach (SaleLine l in e.OldItems) l.PropertyChanged -= OnLineChanged;
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(LineCount));
    }

    void OnLineChanged(object? s, PropertyChangedEventArgs e) => OnPropertyChanged(nameof(Total));

    partial void OnSelectedKlientChanged(Klient? value)
    {
        // si në Access: shkalla e rishitësit e klientit vendoset automatikisht
        if (value != null && value.Id != 0 && Rishitesit.Contains(value.Rishites))
            SelectedRishites = value.Rishites;
    }

    partial void OnSearchTextChanged(string value)
    {
        SearchResults.Clear();
        if (value.Trim().Length < 2) return;
        try { foreach (var a in _art.Search(value, 50)) SearchResults.Add(a); }
        catch (Exception ex) { Status = ex.Message; }
    }

    decimal PriceFor(Artikull a)
    {
        var basePrice = SelectedRishites switch
        {
            "R1" => a.CmimiR1,
            "R2" => a.CmimiR2,
            "R3" => a.CmimiR3,
            _ => a.CmimiDaljes
        };
        return Math.Round(basePrice - basePrice * Skonto / 100m, 2);
    }

    void AddArticle(Artikull a, decimal qty)
    {
        var price = PriceFor(a);
        var existing = Lines.FirstOrDefault(l => l.IdArt == a.Id && l.Price == price);
        if (existing != null) existing.Qty += qty;
        else Lines.Add(new SaleLine
        {
            IdArt = a.Id, Kodi = a.Kodi, Barcodi = a.Barcodi, Emri = a.Emri, Pershkrimi = a.Pershkrimi,
            Firma = a.Firma, Vendi = a.Vendi, Njesia = a.Njesia, CmimiHyrjes = a.CmimiHyrjes, IdCat = a.IdCat,
            Koditvsh = a.Koditvsh, Qty = qty, Price = price
        });
        Status = $"{a.Emri}  x{qty:0.###}  @ {price:0.00}";
        FocusBarcodeRequested?.Invoke();
    }

    [RelayCommand]
    void Scan()
    {
        var code = BarcodeInput.Trim();
        BarcodeInput = "";
        if (code.Length == 0) return;

        decimal qty = 1;
        var lookup = code;

        // barkod peshe: 99 + kodi(5) + kg(2) + gram(3)   (si bcode_AfterUpdate në Access)
        if (code.StartsWith("99") && code.Length >= 12)
        {
            lookup = code.Substring(2, 5);
            var kg = code.Substring(7, 2);
            var gr = code.Substring(9, 3);
            if (!decimal.TryParse($"{kg}.{gr}", NumberStyles.Number, CultureInfo.InvariantCulture, out qty))
                qty = 1;
        }

        Artikull? a;
        try { a = _art.ByBarcode(lookup); }
        catch (Exception ex) { Status = "Gabim në databazë: " + ex.Message; return; }

        if (a == null)
        {
            Status = $"Barkodi '{lookup}' nuk ekziston. Shtoje te Artikuj.";
            System.Media.SystemSounds.Beep.Play();
            FocusBarcodeRequested?.Invoke();
            return;
        }
        AddArticle(a, qty);
    }

    [RelayCommand]
    void AddSearched()
    {
        if (SelectedSearch == null) return;
        AddArticle(SelectedSearch, 1);
    }

    [RelayCommand]
    void RemoveLine()
    {
        if (SelectedLine != null) Lines.Remove(SelectedLine);
        FocusBarcodeRequested?.Invoke();
    }

    [RelayCommand]
    void Clear()
    {
        Lines.Clear();
        PaguarText = "";
        Skonto = 0;
        SelectedKlient = Klientet[0];
        SelectedRishites = "Klient";
        Status = "Gati. Skano barkodin.";
        FocusBarcodeRequested?.Invoke();
    }

    [RelayCommand] void PrintFiscal() => Save(true);
    [RelayCommand] void SaveOnly() => Save(false);

    void Save(bool fiscal)
    {
        if (Lines.Count == 0)
        {
            MessageBox.Show("Nuk keni vendosur asnjë artikull për shitje.", "Asnjë artikull",
                MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }

        var total = Total;
        decimal paguar = total;
        if (!string.IsNullOrWhiteSpace(PaguarText))
        {
            var txt = PaguarText.Trim().Replace(',', '.');
            if (!decimal.TryParse(txt, NumberStyles.Number, CultureInfo.InvariantCulture, out paguar))
            {
                MessageBox.Show("Shuma e paguar nuk është e vlefshme.", "Gabim", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var klient = SelectedKlient is { Id: > 0 } ? SelectedKlient : null;
        if (paguar < total && (klient == null || !_settings.LejonDebi))
        {
            MessageBox.Show("Shuma e paguar është më e vogël se totali, dhe nuk lejohet borxh për këtë shitje.",
                "Pagesë e pamjaftueshme", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sale = new Sale
        {
            User = Session.User, Klient = klient, Rishites = SelectedRishites,
            SkontoPercent = Skonto, Paguar = paguar, Fiscal = fiscal
        };
        sale.Lines.AddRange(Lines);

        try
        {
            var (id, nr) = _fat.SaveSale(sale, _settings.LejonDebi);

            if (fiscal)
            {
                try { AedFiscal.Print(sale.Lines, nr.ToString()); }
                catch (Exception ex)
                {
                    MessageBox.Show("Fatura u ruajt (Nr " + nr + "), por skedari për AED nuk u shkrua:\n" + ex.Message,
                        "Gabim fiskal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            var kusur = paguar > total ? $"   Kusuri: {paguar - total:0.00}" : "";
            Clear();
            Status = $"Fatura Nr {nr} u ruajt. Total: {total:0.00}{kusur}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Shitja NUK u ruajt:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
