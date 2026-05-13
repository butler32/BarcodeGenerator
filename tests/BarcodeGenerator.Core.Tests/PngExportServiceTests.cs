using BarcodeGenerator.Core.Models;
using BarcodeGenerator.Core.Services;
using FluentAssertions;

namespace BarcodeGenerator.Core.Tests;

public class PngExportServiceTests
{
    private readonly ZXingBarcodeGenerator _gen = new();
    private readonly PngExportService _svc = new();

    private BarcodeResult MakeResult(string data = "TEST") =>
        _gen.Generate(new BarcodeRequest(data, BarcodeFormat.DataMatrix));

    [Fact]
    public async Task SaveSingleAsync_CreatesFile()
    {
        var result = MakeResult();
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string path = Path.Combine(dir, "test.png");

        await _svc.SaveSingleAsync(result, path);

        File.Exists(path).Should().BeTrue();
        (new FileInfo(path).Length).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveBatchAsync_CreatesAllFiles()
    {
        var results = new[]
        {
            MakeResult("A001"),
            MakeResult("A002"),
            MakeResult("A003")
        };
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        await _svc.SaveBatchAsync(results, dir, "barcode");

        for (int i = 0; i < results.Length; i++)
        {
            string expected = Path.Combine(dir, $"barcode_{i + 1:D4}.png");
            File.Exists(expected).Should().BeTrue($"file {expected} should exist");
        }
    }

    [Fact]
    public async Task SaveStripAsync_CreatesSingleFile()
    {
        var results = new[] { MakeResult("X1"), MakeResult("X2") };
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string path = Path.Combine(dir, "strip.png");

        await _svc.SaveStripAsync(results, path);

        File.Exists(path).Should().BeTrue();
        (new FileInfo(path).Length).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveStripAsync_EmptyInput_DoesNotThrow()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        string path = Path.Combine(dir, "empty_strip.png");

        await _svc.Invoking(s => s.SaveStripAsync([], path)).Should().NotThrowAsync();
        File.Exists(path).Should().BeFalse("empty strip should not create file");
    }
}
