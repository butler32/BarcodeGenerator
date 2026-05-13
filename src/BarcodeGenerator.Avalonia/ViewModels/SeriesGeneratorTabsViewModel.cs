using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarcodeGenerator.Avalonia.ViewModels;

public partial class SeriesGeneratorTabsViewModel : ViewModelBase
{
    private readonly Func<SeriesGeneratorViewModel> _factory;

    public ObservableCollection<SeriesGeneratorViewModel> Tabs { get; } = [];

    [ObservableProperty]
    private SeriesGeneratorViewModel? _selectedTab;

    public SeriesGeneratorTabsViewModel(Func<SeriesGeneratorViewModel> factory)
    {
        _factory = factory;
        AddTab();
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
        var typed = (SeriesGeneratorViewModel)tab;
        int idx = Tabs.IndexOf(typed);
        Tabs.Remove(typed);
        SelectedTab = Tabs[Math.Clamp(idx, 0, Tabs.Count - 1)];
    }

    public void LoadTabs(IEnumerable<SeriesGeneratorViewModel> tabs)
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
