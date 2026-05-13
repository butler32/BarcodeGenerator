using BarcodeGenerator.Core.Models;
using SkiaSharp;

namespace BarcodeGenerator.Core.Services;

public sealed class PngExportService
{
    private const int LabelFontSize = 14;
    private const int LabelPadding = 6;

    /// <summary>Saves a single barcode PNG to the given file path.</summary>
    public async Task SaveSingleAsync(BarcodeResult result, string filePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, result.PngBytes);
    }

    /// <summary>
    /// Saves each barcode as a separate file: {directory}/{baseName}_{index:D4}.png
    /// </summary>
    public async Task SaveBatchAsync(
        IReadOnlyList<BarcodeResult> results,
        string directory,
        string baseName = "barcode")
    {
        Directory.CreateDirectory(directory);
        var tasks = results.Select((r, i) =>
        {
            string path = Path.Combine(directory, $"{baseName}_{(i + 1):D4}.png");
            return File.WriteAllBytesAsync(path, r.PngBytes);
        });
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Saves all barcodes stacked vertically into a single PNG strip,
    /// with the data string label drawn beneath each barcode.
    /// </summary>
    public async Task SaveStripAsync(
        IReadOnlyList<BarcodeResult> results,
        string filePath,
        int padding = 20,
        bool showLabels = true)
    {
        if (results.Count == 0) return;

        using var paint = new SKPaint();
        using var font = new SKFont(SKTypeface.Default, LabelFontSize);

        int maxWidth = results.Max(r => r.Bitmap.Width);
        int labelHeight = showLabels ? LabelFontSize + LabelPadding * 2 : 0;
        int totalHeight = results.Sum(r => r.Bitmap.Height + labelHeight + padding) + padding;
        int totalWidth = maxWidth + padding * 2;

        using var stripBitmap = new SKBitmap(totalWidth, totalHeight);
        using var canvas = new SKCanvas(stripBitmap);

        canvas.Clear(SKColors.White);

        int y = padding;
        foreach (var result in results)
        {
            int x = (totalWidth - result.Bitmap.Width) / 2;
            canvas.DrawBitmap(result.Bitmap, x, y);
            y += result.Bitmap.Height;

            if (showLabels)
            {
                paint.Color = SKColors.Black;
                paint.IsAntialias = true;

                float textX = totalWidth / 2f;
                float textY = y + LabelPadding + LabelFontSize;
                canvas.DrawText(result.Data, textX, textY, SKTextAlign.Center, font, paint);
                y += labelHeight;
            }

            y += padding;
        }

        using var ms = new MemoryStream();
        stripBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
        byte[] bytes = ms.ToArray();

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, bytes);
    }
}
