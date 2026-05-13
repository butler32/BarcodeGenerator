namespace BarcodeGenerator.Core.Services;

public sealed class SerialCodeService
{
    /// <summary>
    /// Splits multiline input into a list of non-empty trimmed codes.
    /// Supports \n, \r\n, \r separators.
    /// </summary>
    public IReadOnlyList<string> ParseMultiline(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        return input
            .Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }

    /// <summary>
    /// Generates a serial sequence: baseCode + zero-padded counter.
    /// Example: GenerateSeries("ABC", 1, 5, 4) → "ABC0001", "ABC0002", ... "ABC0005"
    /// </summary>
    public IReadOnlyList<string> GenerateSeries(
        string baseCode,
        int startIndex,
        int count,
        int digits)
    {
        ArgumentNullException.ThrowIfNull(baseCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfNegative(digits);

        var result = new List<string>(count);
        for (int i = 0; i < count; i++)
        {
            int index = startIndex + i;
            string suffix = digits > 0
                ? index.ToString($"D{digits}")
                : index.ToString();
            result.Add(baseCode + suffix);
        }
        return result;
    }
}
