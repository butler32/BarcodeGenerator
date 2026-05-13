using System.Text.Json;
using BarcodeGenerator.Core.Models;

namespace BarcodeGenerator.Avalonia.Services;

// ─── State DTOs ────────────────────────────────────────────────────────────

public class AppState
{
    public string SingleTabName { get; set; } = "Один баркод";
    public string MultiTabName { get; set; } = "Несколько баркодов";
    public string SeriesTabName { get; set; } = "Серия баркодов";

    public List<SingleBarcodeTabState> SingleTabs { get; set; } = [];
    public List<MultiBarcodesTabState> MultiTabs { get; set; } = [];
    public List<SeriesGeneratorTabState> SeriesTabs { get; set; } = [];
}

public class SingleBarcodeTabState
{
    public string TabName { get; set; } = "Вкладка";
    public string InputData { get; set; } = string.Empty;
    public BarcodeFormat SelectedFormat { get; set; } = BarcodeFormat.GS1DataMatrix;
    public int ModuleSize { get; set; } = 10;
}

public class MultiBarcodesTabState
{
    public string TabName { get; set; } = "Вкладка";
    public string InputData { get; set; } = string.Empty;
    public BarcodeFormat SelectedFormat { get; set; } = BarcodeFormat.GS1DataMatrix;
    public int ModuleSize { get; set; } = 10;
    public double PreviewSize { get; set; } = 300.0;
}

public class SeriesGeneratorTabState
{
    public string TabName { get; set; } = "Вкладка";
    public string BaseCode { get; set; } = string.Empty;
    public int StartIndex { get; set; } = 1;
    public int Count { get; set; } = 10;
    public int Digits { get; set; } = 4;
    public BarcodeFormat SelectedFormat { get; set; } = BarcodeFormat.GS1DataMatrix;
    public int ModuleSize { get; set; } = 10;
    public double PreviewSize { get; set; } = 300.0;
}

// ─── Service ───────────────────────────────────────────────────────────────

public class AppStateService
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BarcodeGenerator", "state.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppState? Load()
    {
        try
        {
            if (!File.Exists(StatePath)) return null;
            var json = File.ReadAllText(StatePath);
            return JsonSerializer.Deserialize<AppState>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save(AppState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
            File.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOptions));
        }
        catch
        {
            // Persist failures are non-fatal
        }
    }
}
