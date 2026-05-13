using SkiaSharp;

namespace BarcodeGenerator.Core.Models;

public sealed class BarcodeResult : IDisposable
{
    public string Data { get; }
    public SKBitmap Bitmap { get; }
    public byte[] PngBytes { get; }

    public BarcodeResult(string data, SKBitmap bitmap, byte[] pngBytes)
    {
        Data = data;
        Bitmap = bitmap;
        PngBytes = pngBytes;
    }

    public void Dispose() => Bitmap.Dispose();
}
