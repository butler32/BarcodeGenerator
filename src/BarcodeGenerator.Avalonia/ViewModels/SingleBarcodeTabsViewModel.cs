using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

public partial class SingleBarcodeTabsViewModel : ViewModelBase
{
    private readonly Func<SingleBarcodeViewModel> _factory;

    public ObservableCollection<SingleBarcodeViewModel> Tabs { get; } = [];

    [ObservableProperty]
    private SingleBarcodeViewModel? _selectedTab;

    public SingleBarcodeTabsViewModel(Func<SingleBarcodeViewModel> factory)
    {
        _factory = factory;
        AddTab(); // start with one tab
    }

    [RelayCommand]
    private void AddTab()
    {
        var vm = _factory();
        vm.TabName = $"Вкладка {Tabs.Count + 1}";
        vm.CloseRequested += OnCloseRequested;
        Tabs.Add(vm);
        SelectedTab = vm;
    }

    private void OnCloseRequested(SubTabBase tab)
    {
        if (Tabs.Count <= 1) return;
        var typed = (SingleBarcodeViewModel)tab;
        int idx = Tabs.IndexOf(typed);
        Tabs.Remove(typed);
        SelectedTab = Tabs[Math.Clamp(idx, 0, Tabs.Count - 1)];
    }

    public void LoadTabs(IEnumerable<SingleBarcodeViewModel> tabs)
    {
        foreach (var t in Tabs) t.CloseRequested -= OnCloseRequested;
        Tabs.Clear();
        foreach (var t in tabs)
        {
            t.CloseRequested += OnCloseRequested;
            Tabs.Add(t);
        }
        SelectedTab = Tabs.Count > 0 ? Tabs[0] : null;
    }
}
