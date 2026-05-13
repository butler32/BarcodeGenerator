using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;

namespace BarcodeGenerator.Avalonia.Views;

public partial class ResizableImageControl : UserControl
{
    public static readonly StyledProperty<Bitmap?> BitmapSourceProperty =
        AvaloniaProperty.Register<ResizableImageControl, Bitmap?>(nameof(BitmapSource));

    public static readonly StyledProperty<double> ImageSizeProperty =
        AvaloniaProperty.Register<ResizableImageControl, double>(nameof(ImageSize), defaultValue: 300.0);

    public Bitmap? BitmapSource
    {
        get => GetValue(BitmapSourceProperty);
        set => SetValue(BitmapSourceProperty, value);
    }

    public double ImageSize
    {
        get => GetValue(ImageSizeProperty);
        set => SetValue(ImageSizeProperty, value);
    }

    private bool _isDragging;
    private Point _dragStart;
    private double _sizeAtDragStart;

    public ResizableImageControl()
    {
        InitializeComponent();
        // Set cursor in code-behind — Avalonia cursor names differ from WPF
        PART_Grip.Cursor = new Cursor(StandardCursorType.BottomRightCorner);
        PART_Image.Width = ImageSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (PART_Image is null) return; // guard against pre-init calls
        if (change.Property == BitmapSourceProperty)
        {
            PART_Image.Source = change.GetNewValue<Bitmap?>();
        }
        else if (change.Property == ImageSizeProperty)
        {
            PART_Image.Width = change.GetNewValue<double>();
        }
    }

    private void OnGripPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _isDragging = true;
        _dragStart = e.GetPosition(this);
        _sizeAtDragStart = ImageSize;
        e.Pointer.Capture(PART_Grip);
        e.Handled = true;
    }

    private void OnGripPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isDragging = false;
            return;
        }
        var pos = e.GetPosition(this);
        var delta = Math.Max(pos.X - _dragStart.X, pos.Y - _dragStart.Y);
        ImageSize = Math.Clamp(_sizeAtDragStart + delta, 50, 1200);
    }

    private void OnGripPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging) return;
        _isDragging = false;
        e.Pointer.Capture(null);
    }
}
