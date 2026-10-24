using System.IO;
using System.Text.Json;

namespace MarketPrenga.Services;

public sealed class AppConfig
{
    public string DatabasePath { get; set; } = "";
    public string DatabasePassword { get; set; } = "";
    public string AedOutputFile { get; set; } = @"C:\Proshitje Market 1.4\output\fatura.txt";
    public string AedEncoding { get; set; } = "windows-1252";
    public string DecimalSeparator { get; set; } = ".";
    public decimal LowStockThreshold { get; set; } = 5;

    public static AppConfig Current { get; private set; } = new();

    public static void Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Mungon appsettings.json pranë programit.", path);

        var opt = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        Current = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(path), opt) ?? new AppConfig();
    }
}
