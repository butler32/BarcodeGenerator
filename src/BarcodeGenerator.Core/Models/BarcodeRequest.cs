namespace BarcodeGenerator.Core.Models;

public record BarcodeRequest(
    string Data,
    BarcodeFormat Format,
    int ModuleSize = 10,
    int Margin = 4
);
