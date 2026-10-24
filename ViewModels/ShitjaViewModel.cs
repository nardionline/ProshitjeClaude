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
using MarketPrenga.Views;

namespace MarketPrenga.ViewModels;

public partial class ShitjaViewModel : ObservableObject
{
    readonly ArtikujRepo _art = new();
    readonly FaturaRepo _fat = new();
    readonly AppSettings _settings = new SettingsRepo().Load();

    public ObservableCollection<SaleLine> Lines { get; } = new();
    public ObservableCollection<Artikull> Tiles { get; } = new();
    public List<Kategori> Kategorite { get; } = new();
    public List<Klient> Klientet { get; }
    public string[] Rishitesit { get; } = { "Klient", "R1", "R2", "R3" };

    public event Action? FocusBarcodeRequested;

    [ObservableProperty] private string _barcodeInput = "";
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private Kategori? _selectedKategori;
    [ObservableProperty] private string _selectedRishites = "Klient";
    [ObservableProperty] private decimal _skonto;
    [ObservableProperty] private Klient? _selectedKlient;
    [ObservableProperty] private string _paguarText = "";
    [ObservableProperty] private string _status = "Gati. Skano barkodin ose zgjidh një artikull.";

    public decimal Total => Lines.Sum(l => l.Total);
    public int LineCount => Lines.Count;
    public int HeldCount => HeldStore.Items.Count;

    public string KusuriText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PaguarText) || !TryParsePaid(out var p)) return "";
            var t = Total;
            if (p > t) return $"Kusuri: {p - t:N2}";
            if (p < t) return $"Mbetet: {t - p:N2}";
            return "";
        }
    }

    public ShitjaViewModel()
    {
        Klientet = new List<Klient> { new Klient { Id = 0, Emri = "(pa klient)" } };
        try { Klientet.AddRange(new KlientRepo().Search(null)); } catch { }
        _selectedKlient = Klientet[0];

        Kategorite.Add(new Kategori { Id = 0, Emri = "Të gjitha" });
        try { Kategorite.AddRange(new KategoriRepo().All()); } catch { }
        _selectedKategori = Kategorite[0];

        Lines.CollectionChanged += OnLinesChanged;
        RefreshTiles();
    }

    void NotifyTotals()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(LineCount));
        OnPropertyChanged(nameof(KusuriText));
    }

    void OnLinesChanged(object? s, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null) foreach (SaleLine l in e.NewItems) l.PropertyChanged += OnLineChanged;
        if (e.OldItems != null) foreach (SaleLine l in e.OldItems) l.PropertyChanged -= OnLineChanged;
        NotifyTotals();
    }

    void OnLineChanged(object? s, PropertyChangedEventArgs e) => NotifyTotals();

    partial void OnPaguarTextChanged(string value) => OnPropertyChanged(nameof(KusuriText));

    partial void OnSelectedKlientChanged(Klient? value)
    {
        if (value != null && value.Id != 0 && Rishitesit.Contains(value.Rishites))
            SelectedRishites = value.Rishites;
    }

    partial void OnSelectedKategoriChanged(Kategori? value) => RefreshTiles();
    partial void OnSearchTextChanged(string value) => RefreshTiles();

    void RefreshTiles()
    {
        try
        {
            Tiles.Clear();
            foreach (var a in _art.Browse(SelectedKategori?.Id ?? 0, SearchText, 60)) Tiles.Add(a);
        }
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
        Status = $"{a.Emri}  ×{qty:0.###}  @ {price:0.00}";
        FocusBarcodeRequested?.Invoke();
    }

    bool TryParsePaid(out decimal value)
    {
        var txt = (PaguarText ?? "").Trim().Replace(',', '.');
        return decimal.TryParse(txt, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    // ---------- komandat ----------
    [RelayCommand]
    void Scan()
    {
        var code = BarcodeInput.Trim();
        BarcodeInput = "";
        if (code.Length == 0) return;

        decimal qty = 1;
        var lookup = code;

        // barkod peshe: 99 + kodi(5) + kg(2) + gram(3)
        if (code.StartsWith("99") && code.Length >= 12)
        {
            lookup = code.Substring(2, 5);
            if (!decimal.TryParse($"{code.Substring(7, 2)}.{code.Substring(9, 3)}", NumberStyles.Number,
                    CultureInfo.InvariantCulture, out qty)) qty = 1;
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

    [RelayCommand] void AddTile(Artikull? a) { if (a != null) AddArticle(a, 1); }

    [RelayCommand] void Inc(SaleLine? l) { if (l != null) l.Qty += 1; }

    [RelayCommand]
    void Dec(SaleLine? l)
    {
        if (l == null) return;
        if (l.Qty - 1 <= 0) Lines.Remove(l); else l.Qty -= 1;
    }

    [RelayCommand] void RemoveLine(SaleLine? l) { if (l != null) Lines.Remove(l); FocusBarcodeRequested?.Invoke(); }

    [RelayCommand]
    void QuickPay(string? amount)
    {
        PaguarText = amount == "exact" || string.IsNullOrEmpty(amount) ? Total.ToString("0.##", CultureInfo.InvariantCulture) : amount;
    }

    [RelayCommand]
    void Clear()
    {
        Lines.Clear();
        PaguarText = "";
        Skonto = 0;
        SelectedKlient = Klientet[0];
        SelectedRishites = "Klient";
        Status = "Gati. Skano barkodin ose zgjidh një artikull.";
        FocusBarcodeRequested?.Invoke();
    }

    [RelayCommand]
    void Hold()
    {
        if (Lines.Count == 0) return;
        var h = new HeldSale
        {
            Time = DateTime.Now, KlientId = SelectedKlient?.Id ?? 0, Rishites = SelectedRishites, Skonto = Skonto,
            Label = SelectedKlient is { Id: > 0 } ? SelectedKlient.Emri : ""
        };
        h.Lines.AddRange(Lines);
        HeldStore.Add(h);
        Clear();
        OnPropertyChanged(nameof(HeldCount));
        Status = "Shporta u vendos në pritje.";
    }

    [RelayCommand]
    void Recall()
    {
        if (Lines.Count > 0)
        {
            MessageBox.Show("Shporta aktuale nuk është bosh. Vendose në pritje ose pastroje fillimisht.",
                "Kujdes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var w = new HeldWindow { Owner = Application.Current.MainWindow };
        var ok = w.ShowDialog() == true;
        OnPropertyChanged(nameof(HeldCount));
        if (!ok || w.Chosen is not { } h) return;

        foreach (var l in h.Lines) Lines.Add(l);
        SelectedKlient = Klientet.FirstOrDefault(k => k.Id == h.KlientId) ?? Klientet[0];
        SelectedRishites = h.Rishites;
        Skonto = h.Skonto;
        HeldStore.Remove(h);
        OnPropertyChanged(nameof(HeldCount));
        Status = "Shporta u rikthye.";
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
        if (!string.IsNullOrWhiteSpace(PaguarText) && !TryParsePaid(out paguar))
        {
            MessageBox.Show("Shuma e paguar nuk është e vlefshme.", "Gabim", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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
            var (_, nr) = _fat.SaveSale(sale, _settings.LejonDebi);

            if (fiscal)
            {
                try { AedFiscal.Print(sale.Lines, nr.ToString()); }
                catch (Exception ex)
                {
                    MessageBox.Show("Fatura u ruajt (Nr " + nr + "), por skedari për AED nuk u shkrua:\n" + ex.Message,
                        "Gabim fiskal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            var kusur = paguar > total ? $"   ·   Kusuri: {paguar - total:N2}" : "";
            Clear();
            Status = $"✔ Fatura Nr {nr} u ruajt. Total: {total:N2}{kusur}";
            RefreshTiles();   // stoku i përditësuar
        }
        catch (Exception ex)
        {
            MessageBox.Show("Shitja NUK u ruajt:\n" + ex.Message, "Gabim", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
