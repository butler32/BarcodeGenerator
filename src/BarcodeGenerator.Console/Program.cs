// BarcodeGenerator Console — DataMatrix / GS1 barcode generator
// Usage:
//   barcodegen single  --data "..." [--format datamatrix] [--module 10] [--margin 4] [--out ./output]
//   barcodegen batch   --file codes.txt | --data "C1\nC2" [--strip] [--out ./output]
//   barcodegen series  --base "PREFIX" --start 1 --count 10 --digits 4 [--out ./output] [--strip]

using System.CommandLine;
using BarcodeGenerator.Core.Models;
using BarcodeGenerator.Core.Services;

var generator = new ZXingBarcodeGenerator();
var exportService = new PngExportService();
var serialService = new SerialCodeService();

// ---- Shared options --------------------------------------------------------
var formatOption = new Option<string>("--format", () => "datamatrix",
    "Barcode format: datamatrix | gs1datamatrix | gs1_128");
var moduleOption = new Option<int>("--module", () => 10,
    "Module (cell) size in pixels");
var marginOption = new Option<int>("--margin", () => 4,
    "Quiet zone margin in pixels");
var outOption = new Option<string>("--out", () => ".",
    "Output directory");

// ---- 'single' command ------------------------------------------------------
var singleCmd = new Command("single", "Generate a single barcode PNG");
var dataOption = new Option<string>("--data", "Barcode data string") { IsRequired = true };
singleCmd.AddOption(dataOption);
singleCmd.AddOption(formatOption);
singleCmd.AddOption(moduleOption);
singleCmd.AddOption(marginOption);
singleCmd.AddOption(outOption);

singleCmd.SetHandler(async (data, format, module, margin, outDir) =>
{
    var fmt = ParseFormat(format);
    var result = generator.Generate(new BarcodeRequest(data, fmt, module, margin));
    Directory.CreateDirectory(outDir);
    string path = Path.Combine(outDir, "barcode.png");
    await exportService.SaveSingleAsync(result, path);
    Console.WriteLine($"Saved: {path}");
}, dataOption, formatOption, moduleOption, marginOption, outOption);

// ---- 'batch' command -------------------------------------------------------
var batchCmd = new Command("batch", "Generate barcodes from multiline input or a text file");
var fileOption = new Option<string?>("--file", "Path to text file with one code per line");
var batchDataOption = new Option<string?>("--data", "Inline codes separated by newlines");
var stripOption = new Option<bool>("--strip", () => false, "Also save a vertical strip image");
batchCmd.AddOption(fileOption);
batchCmd.AddOption(batchDataOption);
batchCmd.AddOption(formatOption);
batchCmd.AddOption(moduleOption);
batchCmd.AddOption(marginOption);
batchCmd.AddOption(outOption);
batchCmd.AddOption(stripOption);

batchCmd.SetHandler(async (file, batchData, format, module, margin, outDir, strip) =>
{
    string rawInput = file is not null
        ? await File.ReadAllTextAsync(file)
        : batchData ?? string.Empty;

    var codes = serialService.ParseMultiline(rawInput);
    if (codes.Count == 0)
    {
        Console.Error.WriteLine("No codes found in input.");
        return;
    }

    var fmt = ParseFormat(format);
    var requests = codes.Select(c => new BarcodeRequest(c, fmt, module, margin));
    var results = generator.GenerateBatch(requests);

    Directory.CreateDirectory(outDir);
    await exportService.SaveBatchAsync(results, outDir, "barcode");
    Console.WriteLine($"Saved {results.Count} files to: {outDir}");

    if (strip)
    {
        string stripPath = Path.Combine(outDir, "strip.png");
        await exportService.SaveStripAsync(results, stripPath);
        Console.WriteLine($"Strip saved: {stripPath}");
    }
}, fileOption, batchDataOption, formatOption, moduleOption, marginOption, outOption, stripOption);

// ---- 'series' command ------------------------------------------------------
var seriesCmd = new Command("series", "Generate a numbered series of barcodes");
var baseOption = new Option<string>("--base", "Base code prefix") { IsRequired = true };
var startOption = new Option<int>("--start", () => 1, "Start index");
var countOption = new Option<int>("--count", () => 10, "Number of codes");
var digitsOption = new Option<int>("--digits", () => 4, "Width of numeric suffix (zero-padded)");
seriesCmd.AddOption(baseOption);
seriesCmd.AddOption(startOption);
seriesCmd.AddOption(countOption);
seriesCmd.AddOption(digitsOption);
seriesCmd.AddOption(formatOption);
seriesCmd.AddOption(moduleOption);
seriesCmd.AddOption(marginOption);
seriesCmd.AddOption(outOption);
seriesCmd.AddOption(stripOption);

seriesCmd.SetHandler(async ctx =>
{
    var baseCode = ctx.ParseResult.GetValueForOption(baseOption)!;
    var start    = ctx.ParseResult.GetValueForOption(startOption);
    var count    = ctx.ParseResult.GetValueForOption(countOption);
    var digits   = ctx.ParseResult.GetValueForOption(digitsOption);
    var format   = ctx.ParseResult.GetValueForOption(formatOption)!;
    var module   = ctx.ParseResult.GetValueForOption(moduleOption);
    var margin   = ctx.ParseResult.GetValueForOption(marginOption);
    var outDir   = ctx.ParseResult.GetValueForOption(outOption)!;
    var strip    = ctx.ParseResult.GetValueForOption(stripOption);

    var codes = serialService.GenerateSeries(baseCode, start, count, digits);
    var fmt = ParseFormat(format);
    var requests = codes.Select(c => new BarcodeRequest(c, fmt, module, margin));
    var results = generator.GenerateBatch(requests);

    Directory.CreateDirectory(outDir);
    await exportService.SaveBatchAsync(results, outDir, "barcode");
    Console.WriteLine($"Saved {results.Count} files to: {outDir}");

    if (strip)
    {
        string stripPath = Path.Combine(outDir, "strip.png");
        await exportService.SaveStripAsync(results, stripPath);
        Console.WriteLine($"Strip saved: {stripPath}");
    }
});

// ---- Root command ----------------------------------------------------------
var rootCommand = new RootCommand("DataMatrix / GS1 Barcode Generator")
{
    singleCmd,
    batchCmd,
    seriesCmd
};

return await rootCommand.InvokeAsync(args);

// ---------------------------------------------------------------------------
static BarcodeFormat ParseFormat(string value) => value.ToLowerInvariant() switch
{
    "gs1datamatrix" or "gs1dm" => BarcodeFormat.GS1DataMatrix,
    "gs1_128" or "gs1128" or "code128" => BarcodeFormat.GS1_128,
    _ => BarcodeFormat.DataMatrix
};

