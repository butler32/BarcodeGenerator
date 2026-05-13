using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BarcodeGenerator.Avalonia.Services;
using BarcodeGenerator.Avalonia.ViewModels;
using BarcodeGenerator.Core.Services;

namespace BarcodeGenerator.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Compose services
            var dialogService = new DialogService();
            var generator = new ZXingBarcodeGenerator();
            var exportService = new PngExportService();
            var serialService = new SerialCodeService();
            var stateService = new AppStateService();

            var vm = new MainWindowViewModel(
                new SingleBarcodeTabsViewModel(
                    () => new SingleBarcodeViewModel(generator, exportService, dialogService)),
                new MultiBarcodesTabsViewModel(
                    () => new MultiBarcodesViewModel(generator, serialService, exportService, dialogService)),
                new SeriesGeneratorTabsViewModel(
                    () => new SeriesGeneratorViewModel(generator, serialService, exportService, dialogService))
            );

            // Restore saved state
            var state = stateService.Load();
            if (state != null)
            {
                vm.SingleTabName = state.SingleTabName;
                vm.MultiTabName = state.MultiTabName;
                vm.SeriesTabName = state.SeriesTabName;

                if (state.SingleTabs.Count > 0)
                    vm.SingleBarcode.LoadTabs(state.SingleTabs.Select(s =>
                    {
                        var t = new SingleBarcodeViewModel(generator, exportService, dialogService)
                        {
                            TabName = s.TabName,
                            InputData = s.InputData,
                            SelectedFormat = s.SelectedFormat,
                            ModuleSize = s.ModuleSize
                        };
                        return t;
                    }));

                if (state.MultiTabs.Count > 0)
                    vm.MultiBarcodes.LoadTabs(state.MultiTabs.Select(s =>
                    {
                        var t = new MultiBarcodesViewModel(generator, serialService, exportService, dialogService)
                        {
                            TabName = s.TabName,
                            InputData = s.InputData,
                            SelectedFormat = s.SelectedFormat,
                            ModuleSize = s.ModuleSize,
                            PreviewSize = s.PreviewSize
                        };
                        return t;
                    }));

                if (state.SeriesTabs.Count > 0)
                    vm.SeriesGenerator.LoadTabs(state.SeriesTabs.Select(s =>
                    {
                        var t = new SeriesGeneratorViewModel(generator, serialService, exportService, dialogService)
                        {
                            TabName = s.TabName,
                            BaseCode = s.BaseCode,
                            StartIndex = s.StartIndex,
                            Count = s.Count,
                            Digits = s.Digits,
                            SelectedFormat = s.SelectedFormat,
                            ModuleSize = s.ModuleSize,
                            PreviewSize = s.PreviewSize
                        };
                        return t;
                    }));
            }

            var window = new MainWindow { DataContext = vm };
            dialogService.MainWindow = window;
            desktop.MainWindow = window;

            // Save state on close
            window.Closing += (_, _) =>
            {
                var snapshot = new AppState
                {
                    SingleTabName = vm.SingleTabName,
                    MultiTabName = vm.MultiTabName,
                    SeriesTabName = vm.SeriesTabName,
                    SingleTabs = vm.SingleBarcode.Tabs.Select(t => new SingleBarcodeTabState
                    {
                        TabName = t.TabName,
                        InputData = t.InputData,
                        SelectedFormat = t.SelectedFormat,
                        ModuleSize = t.ModuleSize
                    }).ToList(),
                    MultiTabs = vm.MultiBarcodes.Tabs.Select(t => new MultiBarcodesTabState
                    {
                        TabName = t.TabName,
                        InputData = t.InputData,
                        SelectedFormat = t.SelectedFormat,
                        ModuleSize = t.ModuleSize,
                        PreviewSize = t.PreviewSize
                    }).ToList(),
                    SeriesTabs = vm.SeriesGenerator.Tabs.Select(t => new SeriesGeneratorTabState
                    {
                        TabName = t.TabName,
                        BaseCode = t.BaseCode,
                        StartIndex = t.StartIndex,
                        Count = t.Count,
                        Digits = t.Digits,
                        SelectedFormat = t.SelectedFormat,
                        ModuleSize = t.ModuleSize,
                        PreviewSize = t.PreviewSize
                    }).ToList()
                };
                stateService.Save(snapshot);
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}