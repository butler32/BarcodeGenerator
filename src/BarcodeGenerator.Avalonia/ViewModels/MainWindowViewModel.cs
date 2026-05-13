using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SingleBarcodeTabsViewModel SingleBarcode { get; }
    public MultiBarcodesTabsViewModel MultiBarcodes { get; }
    public SeriesGeneratorTabsViewModel SeriesGenerator { get; }

    [ObservableProperty] private string _singleTabName = "Один баркод";
    [ObservableProperty] private string _multiTabName = "Несколько баркодов";
    [ObservableProperty] private string _seriesTabName = "Серия баркодов";

    [ObservableProperty] private bool _isSingleTabRenaming = false;
    [ObservableProperty] private bool _isMultiTabRenaming = false;
    [ObservableProperty] private bool _isSeriesTabRenaming = false;

    [RelayCommand] private void StartSingleTabRename() => IsSingleTabRenaming = true;
    [RelayCommand] private void ConfirmSingleTabRename() => IsSingleTabRenaming = false;
    [RelayCommand] private void StartMultiTabRename() => IsMultiTabRenaming = true;
    [RelayCommand] private void ConfirmMultiTabRename() => IsMultiTabRenaming = false;
    [RelayCommand] private void StartSeriesTabRename() => IsSeriesTabRenaming = true;
    [RelayCommand] private void ConfirmSeriesTabRename() => IsSeriesTabRenaming = false;

    public MainWindowViewModel(
        SingleBarcodeTabsViewModel singleBarcode,
        MultiBarcodesTabsViewModel multiBarcodes,
        SeriesGeneratorTabsViewModel seriesGenerator)
    {
        SingleBarcode = singleBarcode;
        MultiBarcodes = multiBarcodes;
        SeriesGenerator = seriesGenerator;
    }
}
