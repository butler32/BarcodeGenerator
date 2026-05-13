using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BarcodeGenerator.Avalonia.Views;

public partial class SingleBarcodeView : UserControl
{
    private int _savedCaret;
    private TextBox? _lastActiveTextBox;

    public SingleBarcodeView()
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
}
