using BarcodeGenerator.Core.Models;

namespace BarcodeGenerator.Core.Interfaces;

public interface IBarcodeGenerator
{
    BarcodeResult Generate(BarcodeRequest request);
    IReadOnlyList<BarcodeResult> GenerateBatch(IEnumerable<BarcodeRequest> requests);
}
