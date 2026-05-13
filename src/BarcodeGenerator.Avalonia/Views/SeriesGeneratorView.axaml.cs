using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using BarcodeGenerator.Avalonia.ViewModels;

namespace BarcodeGenerator.Avalonia.Views;

public partial class SeriesGeneratorView : UserControl
{
    private int _savedCaret;
    private TextBox? _lastActiveTextBox;

    public SeriesGeneratorView()
    {
        InitializeComponent();
        this.AddHandler(LostFocusEvent, OnAnyLostFocus, RoutingStrategies.Bubble);
    }

    private void OnAnyLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox tb)
        {
            _savedCaret = tb.CaretIndex;
            _lastActiveTextBox = tb;
        }
    }

    private void OnInsertGsClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var grid = btn.Parent as Grid;
        var tb = grid?.Children.OfType<TextBox>().FirstOrDefault();
        if (tb is null) return;
        var idx = _lastActiveTextBox == tb ? _savedCaret : tb.CaretIndex;
        var current = tb.Text ?? string.Empty;
        tb.Text = current.Insert(Math.Clamp(idx, 0, current.Length), "\x1D");
        tb.CaretIndex = idx + 1;
        tb.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Source is TextBox) return;
        if (DataContext is SeriesGeneratorTabsViewModel tabs &&
            tabs.SelectedTab is { IsListMode: false } tab)
        {
            if (e.Key is Key.Left or Key.Up)
            {
                if (tab.PrevCommand.CanExecute(null)) tab.PrevCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key is Key.Right or Key.Down)
            {
                if (tab.NextCommand.CanExecute(null)) tab.NextCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
