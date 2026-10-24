using System.Data;
using MarketPrenga.Models;

namespace MarketPrenga.Data;

public class PerdoruesRepo
{
    public List<Perdorues> All()
    {
        var dt = Db.Table("SELECT lngEmpID, strEmpName, strEmpPassword, strAccess FROM tblEmployees ORDER BY strEmpName");
        return dt.Rows.Cast<DataRow>().Select(r => new Perdorues
        {
            Id = Db.I(r, "lngEmpID"),
            Emri = Db.S(r, "strEmpName"),
            Fjalekalimi = Db.S(r, "strEmpPassword"),
            Access = Db.S(r, "strAccess")
        }).ToList();
    }

    public void LogLogin(int userId)
    {
        try { Db.Exec("INSERT INTO loginlogout (hyrje, idperdorues) VALUES (?, ?)", DateTime.Now, userId); }
        catch { /* tabela e logut nuk është kritike */ }
    }
}

public class KategoriRepo
{
    public List<Kategori> All()
    {
        var dt = Db.Table("SELECT id, Kategoria FROM Kategoria ORDER BY Kategoria");
        return dt.Rows.Cast<DataRow>().Select(r => new Kategori { Id = Db.I(r, "id"), Emri = Db.S(r, "Kategoria") }).ToList();
    }
}

public class ArtikujRepo
{
    const string Sel =
        "SELECT Id, Kodi, Barcodi, Artikulli, Pershkrimi, Firma, [Vendi ne magazine] AS Vendi, " +
        "[Njësia e matjes] AS Njesia, Cmimihyrjes, [Cmimi i daljes] AS CmimiDaljes, CmimiR1, CmimiR2, CmimiR3, " +
        "[Sasia Stock] AS Stock, idcat, Koditvsh FROM Artikulli";

    static Artikull Map(DataRow r) => new()
    {
        Id = Db.I(r, "Id"), Kodi = Db.S(r, "Kodi"), Barcodi = Db.S(r, "Barcodi"), Emri = Db.S(r, "Artikulli"),
        Pershkrimi = Db.S(r, "Pershkrimi"), Firma = Db.S(r, "Firma"), Vendi = Db.S(r, "Vendi"),
        Njesia = Db.S(r, "Njesia"), CmimiHyrjes = Db.D(r, "Cmimihyrjes"), CmimiDaljes = Db.D(r, "CmimiDaljes"),
        CmimiR1 = Db.D(r, "CmimiR1"), CmimiR2 = Db.D(r, "CmimiR2"), CmimiR3 = Db.D(r, "CmimiR3"),
        Stock = Db.D(r, "Stock"), IdCat = Db.I(r, "idcat"), Koditvsh = Db.D(r, "Koditvsh")
    };

    public List<Artikull> Search(string? text, int max = 500)
    {
        DataTable dt;
        if (string.IsNullOrWhiteSpace(text))
            dt = Db.Table(Sel + " ORDER BY Artikulli");
        else
        {
            var like = "%" + text.Trim() + "%";
            dt = Db.Table(Sel + " WHERE Artikulli LIKE ? OR Barcodi LIKE ? OR Kodi LIKE ? ORDER BY Artikulli", like, like, like);
        }
        return dt.Rows.Cast<DataRow>().Take(max).Select(Map).ToList();
    }

    public List<Artikull> Browse(int categoryId, string? text, int max = 60)
    {
        var where = new List<string>();
        var ps = new List<object?>();
        if (categoryId > 0) { where.Add("idcat = ?"); ps.Add(categoryId); }
        if (!string.IsNullOrWhiteSpace(text))
        {
            var like = "%" + text.Trim() + "%";
            where.Add("(Artikulli LIKE ? OR Barcodi LIKE ? OR Kodi LIKE ?)");
            ps.Add(like); ps.Add(like); ps.Add(like);
        }
        var sql = Sel + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "") + " ORDER BY Artikulli";
        return Db.Table(sql, ps.ToArray()).Rows.Cast<DataRow>().Take(max).Select(Map).ToList();
    }

    public Artikull? ByBarcode(string barcode)
    {
        var dt = Db.Table(Sel + " WHERE Barcodi = ?", barcode);
        return dt.Rows.Count == 0 ? null : Map(dt.Rows[0]);
    }

    public void Save(Artikull a)
    {
        if (a.Id == 0)
        {
            Db.Exec(
                "INSERT INTO Artikulli (Kodi, Barcodi, Artikulli, Pershkrimi, Firma, [Vendi ne magazine], [Njësia e matjes], " +
                "Cmimihyrjes, [Cmimi i daljes], CmimiR1, CmimiR2, CmimiR3, [Sasia Stock], idcat, Koditvsh, " +
                "[Data e futjes si artikull], nekase) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                a.Kodi, a.Barcodi, a.Emri, a.Pershkrimi, a.Firma, a.Vendi, a.Njesia,
                a.CmimiHyrjes, a.CmimiDaljes, a.CmimiR1, a.CmimiR2, a.CmimiR3, a.Stock, a.IdCat, a.Koditvsh,
                DateTime.Now, 1);
        }
        else
        {
            Db.Exec(
                "UPDATE Artikulli SET Kodi=?, Barcodi=?, Artikulli=?, Pershkrimi=?, Firma=?, [Vendi ne magazine]=?, " +
                "[Njësia e matjes]=?, Cmimihyrjes=?, [Cmimi i daljes]=?, CmimiR1=?, CmimiR2=?, CmimiR3=?, " +
                "[Sasia Stock]=?, idcat=?, Koditvsh=? WHERE Id=?",
                a.Kodi, a.Barcodi, a.Emri, a.Pershkrimi, a.Firma, a.Vendi, a.Njesia,
                a.CmimiHyrjes, a.CmimiDaljes, a.CmimiR1, a.CmimiR2, a.CmimiR3, a.Stock, a.IdCat, a.Koditvsh, a.Id);
        }
    }

    public void Delete(int id) => Db.Exec("DELETE FROM Artikulli WHERE Id=?", id);
}

public class KlientRepo
{
    const string Sel =
        "SELECT Id, [Emer Mbiemer] AS Emri, Tel, Cel, email, adrese, VlereDebi, Kod, SHkaleRishites, Piketegrumbulluara FROM Klienti";

    static Klient Map(DataRow r) => new()
    {
        Id = Db.I(r, "Id"), Emri = Db.S(r, "Emri"), Tel = Db.S(r, "Tel"), Cel = Db.S(r, "Cel"),
        Email = Db.S(r, "email"), Adresa = Db.S(r, "adrese"), Debi = Db.D(r, "VlereDebi"), Kod = Db.S(r, "Kod"),
        Rishites = string.IsNullOrEmpty(Db.S(r, "SHkaleRishites")) ? "Klient" : Db.S(r, "SHkaleRishites"),
        Pike = Db.I(r, "Piketegrumbulluara")
    };

    public List<Klient> Search(string? text)
    {
        DataTable dt;
        if (string.IsNullOrWhiteSpace(text))
            dt = Db.Table(Sel + " ORDER BY [Emer Mbiemer]");
        else
        {
            var like = "%" + text.Trim() + "%";
            dt = Db.Table(Sel + " WHERE [Emer Mbiemer] LIKE ? OR Tel LIKE ? OR Cel LIKE ? OR Kod LIKE ? ORDER BY [Emer Mbiemer]",
                like, like, like, like);
        }
        return dt.Rows.Cast<DataRow>().Select(Map).ToList();
    }

    public void Save(Klient k)
    {
        if (k.Id == 0)
            Db.Exec("INSERT INTO Klienti ([Emer Mbiemer], Tel, Cel, email, adrese, [Data e blerjes], SHkaleRishites, VlereDebi, Kod, Piketegrumbulluara) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?)",
                k.Emri, k.Tel, k.Cel, k.Email, k.Adresa, DateTime.Now, k.Rishites, k.Debi, k.Kod, k.Pike);
        else
            Db.Exec("UPDATE Klienti SET [Emer Mbiemer]=?, Tel=?, Cel=?, email=?, adrese=?, SHkaleRishites=?, VlereDebi=?, Kod=?, Piketegrumbulluara=? WHERE Id=?",
                k.Emri, k.Tel, k.Cel, k.Email, k.Adresa, k.Rishites, k.Debi, k.Kod, k.Pike, k.Id);
    }

    public void Delete(int id) => Db.Exec("DELETE FROM Klienti WHERE Id=?", id);
}

public class SettingsRepo
{
    /// <summary>Lexon tabelën settings (po/jo). Nëse nuk lexohet, kthen vlera të sigurta.</summary>
    public AppSettings Load()
    {
        var s = new AppSettings();
        try
        {
            var dt = Db.Table("SELECT * FROM settings");
            if (dt.Rows.Count == 0) return s;
            var r = dt.Rows[0];
            bool Po(string col) => dt.Columns.Contains(col) && Db.S(r, col).Trim().Equals("po", StringComparison.OrdinalIgnoreCase);
            s.LejonDebi = Po("LejonDebi");
            s.LejoSistemPikesh = Po("LejoSistemPikesh");
        }
        catch { /* tabela mund të jetë në front-end; përdor parazgjedhjet */ }
        return s;
    }
}

public class FaturaRepo
{
    /// <summary>
    /// Ruan shitjen në transaksion: kokën e faturës, rreshtat, zbret stokun,
    /// përditëson borxhin e klientit dhe krijon numrin e radhës (autonr).
    /// Kthen (Id e faturës, nrauto).
    /// </summary>
    public (int Id, int Nr) SaveSale(Sale s, bool allowDebt)
    {
        if (s.Lines.Count == 0) throw new InvalidOperationException("Nuk ka asnjë artikull për shitje.");

        using var c = Db.Connect();
        using var tx = c.BeginTransaction();
        try
        {
            // numri aktual i faturës = rreshti i fundit në autonr (si Combo93 në Access)
            int nr = Convert.ToInt32(Db.Scalar(c, tx, "SELECT MAX(Id) FROM autonr") ?? 0);
            if (nr == 0)
            {
                Db.Exec(c, tx, "INSERT INTO autonr (nextnum) VALUES (?)", "mik");
                nr = Convert.ToInt32(Db.Scalar(c, tx, "SELECT @@IDENTITY"));
            }

            var now = DateTime.Now;
            var total = s.Total;
            var klient = s.Klient;
            decimal debiPara = klient?.Debi ?? 0m;
            decimal borxhi = (klient != null && allowDebt && s.Paguar < total) ? total - s.Paguar : 0m;
            decimal debiRi = debiPara + borxhi;

            Db.Exec(c, tx,
                "INSERT INTO [Fature Hyrje-Dalje] (nrauto, [Nr i fatures se furnitorit], [Data], [Ora], Skonto, faturahyrese, faturadalese, " +
                "Perdoruesi, Paguar, [Total], Anulluar, Kasa, Rishites, idklient, DetyrimKlienti, DetyrimReal, Oferte, mbyllurxhiron) " +
                "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                nr, nr.ToString(), now, now, (int)Math.Round(s.SkontoPercent), 0, 1,
                s.User?.Emri, s.Paguar, total, "Jo", s.Fiscal ? "Po" : "Jo", s.Rishites,
                klient?.Id ?? 0, debiPara, debiRi, 0, 0);
            int faturaId = Convert.ToInt32(Db.Scalar(c, tx, "SELECT @@IDENTITY"));

            foreach (var l in s.Lines)
            {
                Db.Exec(c, tx,
                    "INSERT INTO [Hyrje-Dalje] ([hyrje-dalje], idart, Kodi, Barcodi, Artikulli, Pershkrimi, Firma, [Vendi ne magazine], " +
                    "[Njësia e matjes], [Cmimi i hyrjes], [Cmimi i daljes], Sasidalje, Sasihyrje, Koditvsh, idcat, nekase, Totali, [data]) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    faturaId, l.IdArt, l.Kodi, l.Barcodi, l.Emri, l.Pershkrimi, l.Firma, l.Vendi,
                    l.Njesia, l.CmimiHyrjes, l.Price, l.Qty, 0, l.Koditvsh, l.IdCat, s.Fiscal ? 1 : 0, l.Total, now);

                Db.Exec(c, tx,
                    "UPDATE Artikulli SET [Sasia Stock] = IIf(IsNull([Sasia Stock]), 0, [Sasia Stock]) - ? WHERE Id = ?",
                    l.Qty, l.IdArt);
            }

            if (klient != null)
            {
                Db.Exec(c, tx,
                    "INSERT INTO Pagesatedetyrimeve (idKlient, Datepagese, Detyrimipaguar, Detyrimiklientit, Detyrimimbetur, Pershkrim, idfatura) " +
                    "VALUES (?,?,?,?,?,?,?)",
                    klient.Id, now, s.Paguar, debiPara, debiRi,
                    $"Detyrim nga fatura me Nr: {nr} me vlere: {total:0.00}", faturaId);

                if (allowDebt)
                    Db.Exec(c, tx, "UPDATE Klienti SET VlereDebi = ? WHERE Id = ?", debiRi, klient.Id);
            }

            // numri i radhës për faturën tjetër
            Db.Exec(c, tx, "INSERT INTO autonr (nextnum) VALUES (?)", "mik");

            tx.Commit();
            return (faturaId, nr);
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
