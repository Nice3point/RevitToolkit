using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Nice3point.Revit.Toolkit.Tests;

/// <summary>
///     A WPF window of the test thread filled with one color, outside the taskbar, with one column of pixels on the screen.
///     Windows composes no frame for a window entirely outside the screen.
/// </summary>
/// <remarks>
///     The color the window shows is the color of the frame Windows composes for it, the last frame WPF presented.
///     A square in its corner pulses on every frame, as the windows of Revit draw while Revit works: Revit drops the frame message only while WPF draws.
/// </remarks>
internal sealed class ColorWindow : IDisposable
{
    private const double WindowSize = 40;
    private const double VisibleWidth = 1;
    private const double PulseSize = 4;
    private const double PulseMinimumOpacity = 0.2;
    private const uint RenderFullContent = 2;
    private const int RedToGreenShift = 8;
    private const int RedToBlueShift = 16;

    private static readonly TimeSpan DrawTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan PulseDuration = TimeSpan.FromMilliseconds(500);

    private readonly Window _window;
    private readonly IntPtr _handle;

    private ColorWindow(Color color)
    {
        _window = new Window
        {
            Width = WindowSize,
            Height = WindowSize,
            Left = SystemParameters.VirtualScreenLeft + VisibleWidth - WindowSize,
            Top = SystemParameters.VirtualScreenTop,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            Background = new SolidColorBrush(color)
        };

        var pulse = new Rectangle
        {
            Width = PulseSize,
            Height = PulseSize,
            Fill = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };

        pulse.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(PulseMinimumOpacity, PulseDuration)
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        });

        _window.Content = pulse;
        _window.Show();
        _handle = new WindowInteropHelper(_window).Handle;
    }

    /// <summary>
    ///     Shows the window and waits until it shows the color.
    /// </summary>
    public static async Task<ColorWindow> ShowAsync(Color color, CancellationToken cancellationToken)
    {
        var window = new ColorWindow(color);
        var drawnColor = await window.WaitForDrawnColorAsync(color, cancellationToken);
        if (drawnColor != color)
        {
            window.Dispose();
            throw new InvalidOperationException($"The window shows {drawnColor} in place of {color}.");
        }

        return window;
    }

    public void Fill(Color color)
    {
        _window.Background = new SolidColorBrush(color);
    }

    /// <summary>
    ///     Returns the color the window shows once it is the expected color, or the color it shows when the time to draw a frame runs out.
    /// </summary>
    public async Task<Color> WaitForDrawnColorAsync(Color expectedColor, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + DrawTimeout;
        var drawnColor = ReadDrawnColor();
        while (drawnColor != expectedColor && DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval, cancellationToken);
            drawnColor = ReadDrawnColor();
        }

        return drawnColor;
    }

    public void Dispose()
    {
        _window.Close();
    }

    private Color ReadDrawnColor()
    {
        GetClientRect(_handle, out var bounds);
        var windowContext = GetDC(_handle);
        var memoryContext = CreateCompatibleDC(windowContext);
        var bitmap = CreateCompatibleBitmap(windowContext, bounds.Right, bounds.Bottom);
        var previousObject = SelectObject(memoryContext, bitmap);
        try
        {
            PrintWindow(_handle, memoryContext, RenderFullContent);
            var pixel = GetPixel(memoryContext, bounds.Right / 2, bounds.Bottom / 2);

            return Color.FromRgb((byte)pixel, (byte)(pixel >> RedToGreenShift), (byte)(pixel >> RedToBlueShift));
        }
        finally
        {
            SelectObject(memoryContext, previousObject);
            DeleteObject(bitmap);
            DeleteDC(memoryContext);
            ReleaseDC(_handle, windowContext);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ClientBounds
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr window, out ClientBounds bounds);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr window, IntPtr context);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr window, IntPtr context, uint flags);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr context);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr context, int width, int height);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr context, IntPtr graphicsObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr graphicsObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr context);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr context, int x, int y);
}
