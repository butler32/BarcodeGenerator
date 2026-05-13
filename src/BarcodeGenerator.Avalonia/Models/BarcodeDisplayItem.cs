using Avalonia.Media.Imaging;
using BarcodeGenerator.Core.Models;

namespace BarcodeGenerator.Avalonia.Models;

/// <summary>
/// Wraps a BarcodeResult with a ready-to-display Avalonia Bitmap.
/// </summary>
public sealed class BarcodeDisplayItem : IDisposable
{
    public BarcodeResult Result { get; }
    public Bitmap Bitmap { get; }
    public string Data => Result.Data;

    public BarcodeDisplayItem(BarcodeResult result)
    {
        Result = result;
        using var ms = new MemoryStream(result.PngBytes);
        Bitmap = new Bitmap(ms);
    }

    public void Dispose()
    {
        Bitmap.Dispose();
        Result.Dispose();
    }
}
