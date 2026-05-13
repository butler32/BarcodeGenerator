using BarcodeGenerator.Core.Models;
using BarcodeGenerator.Core.Services;
using FluentAssertions;

namespace BarcodeGenerator.Core.Tests;

public class ZXingBarcodeGeneratorTests
{
    private readonly ZXingBarcodeGenerator _gen = new();

    [Theory]
    [InlineData("HELLO", BarcodeFormat.DataMatrix)]
    [InlineData("TEST123", BarcodeFormat.DataMatrix)]
    public void Generate_DataMatrix_ReturnsBitmap(string data, BarcodeFormat fmt)
    {
        var result = _gen.Generate(new BarcodeRequest(data, fmt));

        result.Data.Should().Be(data);
        result.PngBytes.Should().NotBeNullOrEmpty();
        result.Bitmap.Width.Should().BeGreaterThan(0);
        result.Bitmap.Height.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Generate_GS1DataMatrix_ReturnsPng()
    {
        var data = "(01)04600439931256(21)SN001";
        var result = _gen.Generate(new BarcodeRequest(data, BarcodeFormat.GS1DataMatrix));

        result.PngBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PngBytes_StartWithPngSignature()
    {
        var result = _gen.Generate(new BarcodeRequest("ABC", BarcodeFormat.DataMatrix));

        // PNG magic bytes: 89 50 4E 47
        result.PngBytes[0].Should().Be(0x89);
        result.PngBytes[1].Should().Be(0x50);
        result.PngBytes[2].Should().Be(0x4E);
        result.PngBytes[3].Should().Be(0x47);
    }

    [Fact]
    public void GenerateBatch_Returns_CorrectCount()
    {
        var requests = new[]
        {
            new BarcodeRequest("A001", BarcodeFormat.DataMatrix),
            new BarcodeRequest("A002", BarcodeFormat.DataMatrix),
            new BarcodeRequest("A003", BarcodeFormat.DataMatrix)
        };

        var results = _gen.GenerateBatch(requests);

        results.Should().HaveCount(3);
        results.Select(r => r.Data).Should().BeEquivalentTo(["A001", "A002", "A003"],
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void ModuleSize_AffectsImageSize()
    {
        var small = _gen.Generate(new BarcodeRequest("TEST", BarcodeFormat.DataMatrix, ModuleSize: 5));
        var large = _gen.Generate(new BarcodeRequest("TEST", BarcodeFormat.DataMatrix, ModuleSize: 20));

        large.Bitmap.Width.Should().BeGreaterThan(small.Bitmap.Width);
    }
}
