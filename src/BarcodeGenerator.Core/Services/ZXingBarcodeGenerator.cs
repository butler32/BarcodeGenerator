using BarcodeGenerator.Core.Interfaces;
using BarcodeGenerator.Core.Models;
using SkiaSharp;
using ZXing.Common;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;
using CoreFmt = BarcodeGenerator.Core.Models.BarcodeFormat;
using EncodeHintType = ZXing.EncodeHintType;
using ZXingFmt = ZXing.BarcodeFormat;

namespace BarcodeGenerator.Core.Services;

public sealed class ZXingBarcodeGenerator : IBarcodeGenerator
{
    public BarcodeResult Generate(BarcodeRequest request)
    {
        var bitmap = RenderBitmap(request);
        var pngBytes = EncodePng(bitmap);
        return new BarcodeResult(request.Data, bitmap, pngBytes);
    }

    public IReadOnlyList<BarcodeResult> GenerateBatch(IEnumerable<BarcodeRequest> requests)
        => requests.Select(Generate).ToList();

    // -----------------------------------------------------------------------

    private static SKBitmap RenderBitmap(BarcodeRequest request)
    {
        var writer = new BarcodeWriter
        {
            Format = ToZXingFormat(request.Format),
            Renderer = new SKBitmapRenderer(),
            Options = BuildOptions(request)
        };

        string data = NeedsGS1Preparation(request.Format)
            ? PrepareGS1Data(request.Data)
            : request.Data;

        try
        {
            return writer.Write(data);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Не удалось закодировать данные в формате {request.Format}. " +
                "Убедитесь, что данные содержат только допустимые символы " +
                "(для GS1-форматов допускается только ASCII).", ex);
        }
    }

    private static EncodingOptions BuildOptions(BarcodeRequest request)
    {
        int dim = 26 * request.ModuleSize;
        var opts = new EncodingOptions
        {
            Width = dim,
            Height = dim,
            Margin = request.Margin,
            PureBarcode = false
        };

        if (request.Format is CoreFmt.DataMatrix or CoreFmt.GS1DataMatrix)
        {
            opts.Hints[EncodeHintType.DATA_MATRIX_SHAPE] =
                ZXing.Datamatrix.Encoder.SymbolShapeHint.FORCE_SQUARE;
        }

        if (request.Format is CoreFmt.GS1DataMatrix or CoreFmt.GS1_128)
        {
            opts.Hints[EncodeHintType.GS1_FORMAT] = true;
        }

        // Enable UTF-8 for plain DataMatrix so non-ASCII characters work
        if (request.Format is CoreFmt.DataMatrix)
        {
            opts.Hints[EncodeHintType.CHARACTER_SET] = "UTF-8";
        }

        return opts;
    }

    private static bool NeedsGS1Preparation(CoreFmt fmt)
        => fmt is CoreFmt.GS1DataMatrix or CoreFmt.GS1_128;

    private static ZXingFmt ToZXingFormat(CoreFmt fmt) => fmt switch
    {
        CoreFmt.DataMatrix    => ZXingFmt.DATA_MATRIX,
        CoreFmt.GS1DataMatrix => ZXingFmt.DATA_MATRIX,
        CoreFmt.GS1_128       => ZXingFmt.CODE_128,
        _                     => throw new ArgumentOutOfRangeException(nameof(fmt))
    };

    /// <summary>
    /// Normalizes GS1 data: converts bracket-format AIs and ensures FNC1 prefix.
    /// </summary>
    private static string PrepareGS1Data(string data)
    {
        if (data.Contains('('))
        {
            data = System.Text.RegularExpressions.Regex.Replace(
                data, @"\((\d+)\)", m => "\x1D" + m.Groups[1].Value);
        }

        if (!data.StartsWith('\x1D'))
            data = "\x1D" + data;

        return data;
    }

    private static byte[] EncodePng(SKBitmap bitmap)
    {
        using var ms = new System.IO.MemoryStream();
        bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
        return ms.ToArray();
    }
}
