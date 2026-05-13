using BarcodeGenerator.Avalonia.Models;
using BarcodeGenerator.Avalonia.Services;
using BarcodeGenerator.Core.Interfaces;
using BarcodeGenerator.Core.Models;
using BarcodeGenerator.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

public partial class SingleBarcodeViewModel : SubTabBase
{
    private readonly IBarcodeGenerator _generator;
    private readonly PngExportService _exportService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _inputData = string.Empty;

    [ObservableProperty]
    private BarcodeFormat _selectedFormat = BarcodeFormat.GS1DataMatrix;

    [ObservableProperty]
    private int _moduleSize = 10;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SavePngCommand))]
    private BarcodeDisplayItem? _currentItem;

    [ObservableProperty]
    private string? _errorMessage;

    public IReadOnlyList<BarcodeFormat> AvailableFormats { get; } =
        Enum.GetValues<BarcodeFormat>();

    public SingleBarcodeViewModel(
        IBarcodeGenerator generator,
        PngExportService exportService,
        IDialogService dialogService)
    {
        _generator = generator;
        _exportService = exportService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private void Generate()
    {
        if (string.IsNullOrWhiteSpace(InputData)) return;

        CurrentItem?.Dispose();
        CurrentItem = null;
        ErrorMessage = null;
        try
        {
            var request = new BarcodeRequest(InputData.Trim(), SelectedFormat, ModuleSize);
            var result = _generator.Generate(request);
            CurrentItem = new BarcodeDisplayItem(result);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SavePng()
    {
        if (CurrentItem is null) return;
        var path = await _dialogService.SaveFileAsync("barcode.png");
        if (path is null) return;
        await _exportService.SaveSingleAsync(CurrentItem.Result, path);
    }

    private bool CanSave() => CurrentItem is not null;
}
