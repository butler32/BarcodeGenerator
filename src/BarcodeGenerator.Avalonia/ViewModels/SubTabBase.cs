using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

/// <summary>
/// Base for any ViewModel that lives inside a named sub-tab.
/// Provides tab name + close request support.
/// </summary>
public abstract partial class SubTabBase : ViewModelBase
{
    [ObservableProperty]
    private string _tabName = "Вкладка";

    [ObservableProperty]
    private bool _isRenaming = false;

    /// <summary>Raised when the user clicks the ✕ button on the tab header.</summary>
    public event Action<SubTabBase>? CloseRequested;

    [RelayCommand]
    private void RequestClose() => CloseRequested?.Invoke(this);

    [RelayCommand]
    private void StartRename() => IsRenaming = true;

    [RelayCommand]
    private void ConfirmRename() => IsRenaming = false;
}
