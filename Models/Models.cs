using CommunityToolkit.Mvvm.ComponentModel;

namespace MarketPrenga.Models;

public class Perdorues
{
    public int Id { get; set; }
    public string Emri { get; set; } = "";
    public string Fjalekalimi { get; set; } = "";
    public string Access { get; set; } = "";
}

public class Kategori
{
    public int Id { get; set; }
    public string Emri { get; set; } = "";
}

public class Artikull
{
    public int Id { get; set; }
    public string Kodi { get; set; } = "";
    public string Barcodi { get; set; } = "";
    public string Emri { get; set; } = "";          // Artikulli.Artikulli
    public string Pershkrimi { get; set; } = "";
    public string Firma { get; set; } = "";
    public string Vendi { get; set; } = "";         // Vendi ne magazine
    public string Njesia { get; set; } = "cope";    // Njësia e matjes
    public decimal CmimiHyrjes { get; set; }
    public decimal CmimiDaljes { get; set; }
    public decimal CmimiR1 { get; set; }
    public decimal CmimiR2 { get; set; }
    public decimal CmimiR3 { get; set; }
    public decimal Stock { get; set; }
    public int IdCat { get; set; }
    public decimal Koditvsh { get; set; } = 1;      // departamenti (DPT) për AED
}

public class Klient
{
    public int Id { get; set; }
    public string Emri { get; set; } = "";          // Emer Mbiemer
    public string Tel { get; set; } = "";
    public string Cel { get; set; } = "";
    public string Email { get; set; } = "";
    public string Adresa { get; set; } = "";
    public decimal Debi { get; set; }               // VlereDebi
    public string Kod { get; set; } = "";
    public string Rishites { get; set; } = "Klient"; // SHkaleRishites
    public int Pike { get; set; }                   // Piketegrumbulluara
    public override string ToString() => Emri;
}

public class AppSettings
{
    public bool LejonDebi { get; set; }
    public bool LejoSistemPikesh { get; set; }
}

public partial class SaleLine : ObservableObject
{
    public int IdArt { get; init; }
    public string Kodi { get; init; } = "";
    public string Barcodi { get; init; } = "";
    public string Emri { get; init; } = "";
    public string Pershkrimi { get; init; } = "";
    public string Firma { get; init; } = "";
    public string Vendi { get; init; } = "";
    public string Njesia { get; init; } = "";
    public decimal CmimiHyrjes { get; init; }
    public int IdCat { get; init; }
    public decimal Koditvsh { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _qty = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _price;

    public decimal Total => Math.Round(Qty * Price, 2);
}

public class Sale
{
    public List<SaleLine> Lines { get; } = new();
    public Perdorues? User { get; set; }
    public Klient? Klient { get; set; }
    public string Rishites { get; set; } = "Klient";
    public decimal SkontoPercent { get; set; }
    public decimal Paguar { get; set; }
    public bool Fiscal { get; set; }
    public decimal Total => Lines.Sum(l => l.Total);
}

public class Furnitor
{
    public int Id { get; set; }
    public string Emri { get; set; } = "";
    public string Tel { get; set; } = "";
    public string Cel { get; set; } = "";
    public string Email { get; set; } = "";
    public string Adresa { get; set; } = "";
    public decimal Detyrim { get; set; }
}

public class FaturaRow
{
    public int Id { get; set; }
    public string Nr { get; set; } = "";
    public DateTime Data { get; set; }
    public string User { get; set; } = "";
    public string Klient { get; set; } = "";
    public int IdKlient { get; set; }
    public decimal Total { get; set; }
    public decimal Paguar { get; set; }
    public bool Anulluar { get; set; }
    public bool Fiskale { get; set; }
    public string Statusi => Anulluar ? "Anuluar" : (Fiskale ? "Fiskale" : "Ruajtur");
}

public record DayTotal(DateTime Date, int Count, decimal Total)
{
    public string Dita => Date.ToString("ddd dd.MM.yyyy");
}
public record TopItem(string Name, decimal Qty, decimal Total);
public record UserTotal(string User, int Count, decimal Total);
public record StockRow(string Name, decimal Stock);
public record DayBar(string Label, decimal Value, double BarHeight);

public class HeldSale
{
    public string Label { get; set; } = "";
    public DateTime Time { get; set; } = DateTime.Now;
    public int KlientId { get; set; }
    public string Rishites { get; set; } = "Klient";
    public decimal Skonto { get; set; }
    public List<SaleLine> Lines { get; set; } = new();
    public decimal Total => Lines.Sum(l => l.Total);
    public string Display => $"{Time:HH:mm}   •   {Lines.Count} rreshta   •   {Total:N2}" +
                             (string.IsNullOrEmpty(Label) ? "" : $"   •   {Label}");
}
