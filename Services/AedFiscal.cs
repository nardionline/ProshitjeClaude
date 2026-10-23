using System.Globalization;
using System.IO;
using System.Text;
using MarketPrenga.Models;

namespace MarketPrenga.Services;

/// <summary>
/// Gjeneron skedarin tekst për kasën fiskale AED, njësoj si raporti Access "fiskaleAED":
///   CLEAR / KEY REG / inp num='N',TERM=TSFATS / SALE DPT=..,qty=..,PRICE=..,DES='..' / SUBTOT / CLOSE TENDER=1
/// </summary>
public static class AedFiscal
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Build(IEnumerable<SaleLine> lines, string receiptNumber)
    {
        var cfg = AppConfig.Current;
        string Num(string s) => cfg.DecimalSeparator == "." ? s : s.Replace(".", cfg.DecimalSeparator);

        var sb = new StringBuilder();
        sb.Append("CLEAR \r\n");
        sb.Append("KEY REG   \r\n");
        sb.Append($"inp num='{receiptNumber}',TERM=TSFATS\r\n");

        // grupim sipas (DPT, artikull, çmim) si në query-n e Access
        var groups = lines
            .GroupBy(l => (l.Koditvsh, l.Emri, l.Price))
            .OrderBy(g => g.Key.Emri);

        foreach (var g in groups)
        {
            var qty = g.Sum(x => x.Qty);
            var des = g.Key.Emri.Replace("'", " ");
            sb.Append($"SALE DPT={g.Key.Koditvsh.ToString("0.##", Inv)}," +
                      $"qty={Num(qty.ToString("0.00", Inv))}," +
                      $"PRICE={Num(g.Key.Price.ToString("0.##", Inv))}," +
                      $"DES='{des}'\r\n");
        }

        sb.Append("SUBTOT\r\n");
        sb.Append("CLOSE TENDER=1\r\n");
        return sb.ToString();
    }

    public static void Print(IEnumerable<SaleLine> lines, string receiptNumber)
    {
        var cfg = AppConfig.Current;
        var text = Build(lines, receiptNumber);
        var dir = Path.GetDirectoryName(cfg.AedOutputFile);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        Encoding enc;
        try { enc = Encoding.GetEncoding(cfg.AedEncoding); }
        catch { enc = Encoding.GetEncoding(1252); }

        File.WriteAllText(cfg.AedOutputFile, text, enc);
    }
}
