using BarcodeGenerator.Core.Services;
using FluentAssertions;

namespace BarcodeGenerator.Core.Tests;

public class SerialCodeServiceTests
{
    private readonly SerialCodeService _svc = new();

    [Fact]
    public void ParseMultiline_SplitsCorrectly()
    {
        var result = _svc.ParseMultiline("CODE1\nCODE2\nCODE3");
        result.Should().BeEquivalentTo(["CODE1", "CODE2", "CODE3"]);
    }

    [Fact]
    public void ParseMultiline_IgnoresEmptyLines()
    {
        var result = _svc.ParseMultiline("A\n\nB\n\nC");
        result.Should().BeEquivalentTo(["A", "B", "C"]);
    }

    [Fact]
    public void ParseMultiline_ReturnsEmpty_WhenInputIsBlank()
    {
        _svc.ParseMultiline("   ").Should().BeEmpty();
        _svc.ParseMultiline("").Should().BeEmpty();
    }

    [Fact]
    public void GenerateSeries_ProducesCorrectCount()
    {
        var result = _svc.GenerateSeries("BASE", 1, 5, 4);
        result.Should().HaveCount(5);
    }

    [Fact]
    public void GenerateSeries_AppliesLeadingZeros()
    {
        var result = _svc.GenerateSeries("ABC", 1, 3, 4);
        result.Should().BeEquivalentTo(["ABC0001", "ABC0002", "ABC0003"],
            opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void GenerateSeries_StartsFromCustomIndex()
    {
        var result = _svc.GenerateSeries("X", 10, 2, 3);
        result.Should().BeEquivalentTo(["X010", "X011"], opts => opts.WithStrictOrdering());
    }

    [Fact]
    public void GenerateSeries_ZeroDigits_AppendsRawNumber()
    {
        var result = _svc.GenerateSeries("PREFIX", 5, 2, 0);
        result.Should().BeEquivalentTo(["PREFIX5", "PREFIX6"], opts => opts.WithStrictOrdering());
    }
}
