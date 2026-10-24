using System.Data;
using MarketPrenga.Models;

namespace MarketPrenga.Data;

public class ReportRepo
{
    record Line(int F, DateTime D, string U, string A, decimal Q, decimal P)
    {
        public decimal Amount => Q * P;
    }

    List<Line> Lines(DateTime from, DateTime to)
    {
        var dt = Db.Table(
            "SELECT f.Id AS FId, f.[Data] AS D, f.Perdoruesi AS U, h.Artikulli AS A, h.Sasidalje AS Q, h.[Cmimi i daljes] AS P " +
            "FROM [Fature Hyrje-Dalje] AS f INNER JOIN [Hyrje-Dalje] AS h ON f.Id = h.[hyrje-dalje] " +
            "WHERE f.faturadalese = 1 AND f.Anulluar = 'Jo' AND IIf(IsNull(f.Oferte), 0, f.Oferte) = 0 " +
            "AND f.[Data] >= ? AND f.[Data] < ?",
            from.Date, to.Date.AddDays(1));

        return dt.Rows.Cast<DataRow>().Select(r => new Line(
            Db.I(r, "FId"),
            r["D"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(r["D"]),
            Db.S(r, "U"), Db.S(r, "A"), Db.D(r, "Q"), Db.D(r, "P"))).ToList();
    }

    public List<DayTotal> Daily(DateTime from, DateTime to) =>
        Lines(from, to)
            .GroupBy(l => l.D.Date)
            .Select(g => new DayTotal(g.Key, g.Select(x => x.F).Distinct().Count(), g.Sum(x => x.Amount)))
            .OrderBy(d => d.Date).ToList();

    public List<TopItem> Top(DateTime from, DateTime to, int n) =>
        Lines(from, to)
            .GroupBy(l => l.A)
            .Select(g => new TopItem(g.Key, g.Sum(x => x.Q), g.Sum(x => x.Amount)))
            .OrderByDescending(t => t.Total).Take(n).ToList();

    public List<UserTotal> ByUser(DateTime from, DateTime to) =>
        Lines(from, to)
            .GroupBy(l => l.U)
            .Select(g => new UserTotal(string.IsNullOrEmpty(g.Key) ? "(pa emër)" : g.Key,
                g.Select(x => x.F).Distinct().Count(), g.Sum(x => x.Amount)))
            .OrderByDescending(u => u.Total).ToList();

    public decimal DebtTotal()
    {
        var o = Db.Scalar("SELECT SUM(VlereDebi) FROM Klienti WHERE VlereDebi > 0");
        return o == null ? 0m : Convert.ToDecimal(o);
    }

    public List<StockRow> LowStock(decimal threshold)
    {
        var dt = Db.Table("SELECT Artikulli AS N, [Sasia Stock] AS S FROM Artikulli WHERE [Sasia Stock] <= ? ORDER BY [Sasia Stock]", threshold);
        return dt.Rows.Cast<DataRow>().Select(r => new StockRow(Db.S(r, "N"), Db.D(r, "S"))).ToList();
    }
}

public class FaturatRepo
{
    public List<FaturaRow> List(DateTime from, DateTime to)
    {
        var names = Db.Table("SELECT Id, [Emer Mbiemer] AS N FROM Klienti").Rows.Cast<DataRow>()
            .ToDictionary(r => Db.I(r, "Id"), r => Db.S(r, "N"));

        var dt = Db.Table(
            "SELECT f.Id AS FId, f.nrauto AS Nr, f.[Data] AS D, f.Perdoruesi AS U, f.Anulluar AS An, f.Kasa AS K, " +
            "f.Paguar AS Pg, f.idklient AS Ik, SUM(h.Sasidalje * h.[Cmimi i daljes]) AS T " +
            "FROM [Fature Hyrje-Dalje] AS f LEFT JOIN [Hyrje-Dalje] AS h ON f.Id = h.[hyrje-dalje] " +
            "WHERE f.faturadalese = 1 AND IIf(IsNull(f.Oferte), 0, f.Oferte) = 0 AND f.[Data] >= ? AND f.[Data] < ? " +
            "GROUP BY f.Id, f.nrauto, f.[Data], f.Perdoruesi, f.Anulluar, f.Kasa, f.Paguar, f.idklient " +
            "ORDER BY f.Id DESC",
            from.Date, to.Date.AddDays(1));

        return dt.Rows.Cast<DataRow>().Select(r =>
        {
            var ik = Db.I(r, "Ik");
            return new FaturaRow
            {
                Id = Db.I(r, "FId"), Nr = Db.S(r, "Nr"),
                Data = r["D"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(r["D"]),
                User = Db.S(r, "U"), IdKlient = ik, Klient = names.TryGetValue(ik, out var n) ? n : "",
                Total = Db.D(r, "T"), Paguar = Db.D(r, "Pg"),
                Anulluar = Db.S(r, "An").Equals("Po", StringComparison.OrdinalIgnoreCase),
                Fiskale = Db.S(r, "K").Equals("Po", StringComparison.OrdinalIgnoreCase)
            };
        }).ToList();
    }

    /// <summary>Anulon faturën: kthen stokun dhe zbret borxhin që kishte krijuar te klienti.</summary>
    public void Cancel(int faturaId)
    {
        using var c = Db.Connect();
        using var tx = c.BeginTransaction();
        try
        {
            var h = Db.Table(c, tx,
                "SELECT Anulluar, idklient, DetyrimKlienti, DetyrimReal FROM [Fature Hyrje-Dalje] WHERE Id = ?", faturaId);
            if (h.Rows.Count == 0) throw new InvalidOperationException("Fatura nuk u gjet.");
            var r = h.Rows[0];
            if (Db.S(r, "Anulluar").Equals("Po", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Fatura është anuluar tashmë.");

            Db.Exec(c, tx,
                "UPDATE Artikulli INNER JOIN [Hyrje-Dalje] ON Artikulli.Id = [Hyrje-Dalje].idart " +
                "SET Artikulli.[Sasia Stock] = IIf(IsNull(Artikulli.[Sasia Stock]), 0, Artikulli.[Sasia Stock]) + " +
                "IIf(IsNull([Hyrje-Dalje].Sasidalje), 0, [Hyrje-Dalje].Sasidalje) " +
                "WHERE [Hyrje-Dalje].[hyrje-dalje] = ?", faturaId);

            var ik = Db.I(r, "idklient");
            var borxhiKrijuar = Db.D(r, "DetyrimReal") - Db.D(r, "DetyrimKlienti");
            if (ik > 0 && borxhiKrijuar > 0)
                Db.Exec(c, tx,
                    "UPDATE Klienti SET VlereDebi = IIf(IsNull(VlereDebi), 0, VlereDebi) - ? WHERE Id = ?",
                    borxhiKrijuar, ik);

            Db.Exec(c, tx, "UPDATE [Fature Hyrje-Dalje] SET Anulluar = 'Po' WHERE Id = ?", faturaId);
            tx.Commit();
        }
        catch { tx.Rollback(); throw; }
    }
}

public class FurnitorRepo
{
    const string Sel = "SELECT Id, [Emer Mbiemer] AS Emri, Tel, Cel, email, adrese, Detyrim FROM Furnitori";

    static Furnitor Map(DataRow r) => new()
    {
        Id = Db.I(r, "Id"), Emri = Db.S(r, "Emri"), Tel = Db.S(r, "Tel"), Cel = Db.S(r, "Cel"),
        Email = Db.S(r, "email"), Adresa = Db.S(r, "adrese"), Detyrim = Db.D(r, "Detyrim")
    };

    public List<Furnitor> Search(string? text)
    {
        DataTable dt;
        if (string.IsNullOrWhiteSpace(text)) dt = Db.Table(Sel + " ORDER BY [Emer Mbiemer]");
        else
        {
            var like = "%" + text.Trim() + "%";
            dt = Db.Table(Sel + " WHERE [Emer Mbiemer] LIKE ? OR Tel LIKE ? OR Cel LIKE ? ORDER BY [Emer Mbiemer]", like, like, like);
        }
        return dt.Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public void Save(Furnitor f)
    {
        if (f.Id == 0)
            Db.Exec("INSERT INTO Furnitori ([Emer Mbiemer], Tel, Cel, email, adrese, [Data e furnizimit], Detyrim) VALUES (?,?,?,?,?,?,?)",
                f.Emri, f.Tel, f.Cel, f.Email, f.Adresa, DateTime.Now, f.Detyrim);
        else
            Db.Exec("UPDATE Furnitori SET [Emer Mbiemer]=?, Tel=?, Cel=?, email=?, adrese=?, Detyrim=? WHERE Id=?",
                f.Emri, f.Tel, f.Cel, f.Email, f.Adresa, f.Detyrim, f.Id);
    }

    public void Delete(int id) => Db.Exec("DELETE FROM Furnitori WHERE Id=?", id);
}

public class DebtRepo
{
    /// <summary>Regjistron pagesë borxhi të klientit; kthen borxhin e ri.</summary>
    public decimal PayClient(int klientId, decimal amount)
    {
        using var c = Db.Connect();
        using var tx = c.BeginTransaction();
        try
        {
            var o = Db.Scalar(c, tx, "SELECT VlereDebi FROM Klienti WHERE Id = ?", klientId);
            var old = o == null ? 0m : Convert.ToDecimal(o);
            var neu = Math.Max(0m, old - amount);

            Db.Exec(c, tx,
                "INSERT INTO Pagesatedetyrimeve (idKlient, Datepagese, Detyrimiklientit, Detyrimipaguar, Detyrimimbetur, Pershkrim, TipPagese) " +
                "VALUES (?,?,?,?,?,?,?)",
                klientId, DateTime.Now, old, amount, neu, "Pagesë borxhi", "Pagese");
            Db.Exec(c, tx, "UPDATE Klienti SET VlereDebi = ? WHERE Id = ?", neu, klientId);
            tx.Commit();
            return neu;
        }
        catch { tx.Rollback(); throw; }
    }
}
