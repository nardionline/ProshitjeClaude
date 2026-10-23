using System.Data;
using System.Data.OleDb;
using System.IO;

namespace MarketPrenga.Data;

/// <summary>Qasje e thjeshtë në databazën Access (.mdb/.accdb) me OleDb.</summary>
public static class Db
{
    static string _cs = "";

    public static void Init(string path, string password)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Skedari i databazës nuk u gjet. Kontrollo appsettings.json.", path);

        var pw = string.IsNullOrEmpty(password) ? "" : $"Jet OLEDB:Database Password={password};";
        string[] providers = { "Microsoft.Jet.OLEDB.4.0", "Microsoft.ACE.OLEDB.12.0", "Microsoft.ACE.OLEDB.16.0" };
        Exception? last = null;

        foreach (var p in providers)
        {
            var cs = $"Provider={p};Data Source={path};{pw}";
            try
            {
                using var c = new OleDbConnection(cs);
                c.Open();
                _cs = cs;
                return;
            }
            catch (Exception ex) { last = ex; }
        }
        throw new InvalidOperationException(
            "Nuk u hap databaza Access. Programi duhet të punojë si 32-bit (x86) dhe Windows duhet të ketë Jet 4.0 " +
            "ose Microsoft Access Database Engine.", last);
    }

    public static OleDbConnection Connect()
    {
        var c = new OleDbConnection(_cs);
        c.Open();
        return c;
    }

    static void AddParams(OleDbCommand cmd, object?[] p)
    {
        foreach (var v in p)
        {
            var prm = new OleDbParameter();
            switch (v)
            {
                case null: prm.Value = DBNull.Value; break;
                case string s: prm.Value = s.Length == 0 ? DBNull.Value : s; break;
                case DateTime d:
                    prm.OleDbType = OleDbType.Date;
                    prm.Value = new DateTime(d.Ticks - d.Ticks % TimeSpan.TicksPerSecond);
                    break;
                case decimal m:
                    prm.OleDbType = OleDbType.Double;
                    prm.Value = (double)m;
                    break;
                default: prm.Value = v; break;
            }
            cmd.Parameters.Add(prm);
        }
    }

    static OleDbCommand Cmd(OleDbConnection c, OleDbTransaction? t, string sql, object?[] p)
    {
        var cmd = new OleDbCommand(sql, c, t);
        AddParams(cmd, p);
        return cmd;
    }

    public static DataTable Table(string sql, params object?[] p)
    {
        using var c = Connect();
        return Table(c, null, sql, p);
    }

    public static DataTable Table(OleDbConnection c, OleDbTransaction? t, string sql, params object?[] p)
    {
        using var cmd = Cmd(c, t, sql, p);
        using var da = new OleDbDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    public static int Exec(string sql, params object?[] p)
    {
        using var c = Connect();
        return Exec(c, null, sql, p);
    }

    public static int Exec(OleDbConnection c, OleDbTransaction? t, string sql, params object?[] p)
    {
        using var cmd = Cmd(c, t, sql, p);
        return cmd.ExecuteNonQuery();
    }

    public static object? Scalar(OleDbConnection c, OleDbTransaction? t, string sql, params object?[] p)
    {
        using var cmd = Cmd(c, t, sql, p);
        var o = cmd.ExecuteScalar();
        return o == DBNull.Value ? null : o;
    }

    public static object? Scalar(string sql, params object?[] p)
    {
        using var c = Connect();
        return Scalar(c, null, sql, p);
    }

    // ---- lexim i sigurt i vlerave ----
    public static string S(DataRow r, string col) => r[col] is DBNull or null ? "" : Convert.ToString(r[col]) ?? "";
    public static decimal D(DataRow r, string col) => r[col] is DBNull or null ? 0m : Convert.ToDecimal(r[col]);
    public static int I(DataRow r, string col) => r[col] is DBNull or null ? 0 : Convert.ToInt32(r[col]);
}
