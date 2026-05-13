using BarcodeGenerator.Avalonia.Models;
using BarcodeGenerator.Avalonia.Services;
using BarcodeGenerator.Core.Interfaces;
using BarcodeGenerator.Core.Models;
using BarcodeGenerator.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

public partial class MultiBarcodesViewModel : BarcodeResultsViewModelBase
{
    private readonly IBarcodeGenerator _generator;
    private readonly SerialCodeService _serialService;

    [ObservableProperty]
    private string _inputData = string.Empty;

    [ObservableProperty]
    private BarcodeFormat _selectedFormat = BarcodeFormat.GS1DataMatrix;

    [ObservableProperty]
    private int _moduleSize = 10;

    public IReadOnlyList<BarcodeFormat> AvailableFormats { get; } =
        Enum.GetValues<BarcodeFormat>();

    public MultiBarcodesViewModel(
        IBarcodeGenerator generator,
        SerialCodeService serialService,
        PngExportService exportService,
        IDialogService dialogService)
        : base(exportService, dialogService)
    {
        _generator = generator;
        _serialService = serialService;
    }

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(InputData)) return;

        var codes = _serialService.ParseMultiline(InputData);
        if (codes.Count == 0) return;

        try
        {
            var requests = codes.Select(c => new BarcodeRequest(c, SelectedFormat, ModuleSize));
            var results = _generator.GenerateBatch(requests);
            LoadResults(results.Select(r => new BarcodeDisplayItem(r)));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
