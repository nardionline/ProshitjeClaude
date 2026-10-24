using System.IO;
using System.Text.Json;
using MarketPrenga.Models;

namespace MarketPrenga.Services;

/// <summary>Shportat në pritje; ruhen në %AppData%\MarketPrenga\held.json që mbijetojnë mbylljes së programit.</summary>
public static class HeldStore
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MarketPrenga", "held.json");

    public static List<HeldSale> Items { get; } = Load();

    static List<HeldSale> Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<List<HeldSale>>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { }
        return new();
    }

    static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Items));
        }
        catch { }
    }

    public static void Add(HeldSale h) { Items.Add(h); Save(); }
    public static void Remove(HeldSale h) { Items.Remove(h); Save(); }
}
