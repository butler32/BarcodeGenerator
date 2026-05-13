using System.Collections.ObjectModel;
using BarcodeGenerator.Avalonia.Models;
using BarcodeGenerator.Avalonia.Services;
using BarcodeGenerator.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

/// <summary>
/// Base ViewModel for any view that displays a collection of barcode results
/// with list/single-item navigation modes and save operations.
/// </summary>
public abstract partial class BarcodeResultsViewModelBase : SubTabBase
{
    protected readonly PngExportService _exportService;
    protected readonly IDialogService _dialogService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentItem), nameof(CurrentInfo))]
    [NotifyCanExecuteChangedFor(nameof(PrevCommand), nameof(NextCommand))]
    private int _currentIndex;

    [ObservableProperty]
    private bool _isListMode = false;

    [ObservableProperty]
    private double _previewSize = 300.0;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<BarcodeDisplayItem> Results { get; } = [];

    public BarcodeDisplayItem? CurrentItem =>
        Results.Count > 0 && CurrentIndex >= 0 && CurrentIndex < Results.Count
            ? Results[CurrentIndex]
            : null;

    public string CurrentInfo => Results.Count > 0
        ? $"{CurrentIndex + 1} из {Results.Count}: \"{CurrentItem?.Data}\""
        : string.Empty;

    protected BarcodeResultsViewModelBase(PngExportService exportService, IDialogService dialogService)
    {
        _exportService = exportService;
        _dialogService = dialogService;
    }

    protected void LoadResults(IEnumerable<BarcodeDisplayItem> items)
    {
        ErrorMessage = null;
        Results.Clear();
        foreach (var item in items)
            Results.Add(item);
        CurrentIndex = 0;
        // Force UI refresh when CurrentIndex was already 0 before generate
        OnPropertyChanged(nameof(CurrentItem));
        OnPropertyChanged(nameof(CurrentInfo));
        PrevCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    // ------------------------------------------------------------------
    // Navigation

    [RelayCommand(CanExecute = nameof(CanGoPrev))]
    private void Prev() => CurrentIndex--;

    private bool CanGoPrev() => CurrentIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => CurrentIndex++;

    private bool CanGoNext() => CurrentIndex < Results.Count - 1;

    // ------------------------------------------------------------------
    // Save operations

    [RelayCommand(CanExecute = nameof(HasResults))]
    private async Task SaveCurrent()
    {
        if (CurrentItem is null) return;
        var path = await _dialogService.SaveFileAsync($"barcode_{CurrentIndex + 1:D4}.png");
        if (path is null) return;
        await _exportService.SaveSingleAsync(CurrentItem.Result, path);
    }

    [RelayCommand(CanExecute = nameof(HasResults))]
    private async Task SaveAll()
    {
        var folder = await _dialogService.SaveFolderAsync("Select folder to save all barcodes");
        if (folder is null) return;
        var results = Results.Select(r => r.Result).ToList();
        await _exportService.SaveBatchAsync(results, folder, "barcode");
    }

    [RelayCommand(CanExecute = nameof(HasResults))]
    private async Task SaveStrip()
    {
        var path = await _dialogService.SaveFileAsync("strip.png");
        if (path is null) return;
        var results = Results.Select(r => r.Result).ToList();
        await _exportService.SaveStripAsync(results, path);
    }

    private bool HasResults() => Results.Count > 0;

    partial void OnCurrentIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CurrentItem));
        OnPropertyChanged(nameof(CurrentInfo));
    }
}
