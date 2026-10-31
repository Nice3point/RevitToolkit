using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.Storage.Xps;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace Nice3point.Revit.Toolkit.Tests;

/// <summary>
///     Represents a WPF window of the test thread filled with one color, outside the taskbar, with one column of pixels on the screen.
/// </summary>
/// <remarks>
///     Windows composes no frame for a window entirely outside the screen.
///     The color the window shows is the color of the frame Windows composes for it, the last frame WPF presented.
///     A square in its corner pulses on every frame, as the ribbon and the panes of Revit draw while Revit works: Revit discards the frame notification only while WPF draws.
/// </remarks>
internal sealed class ColorWindow : IDisposable
{
    private const double WindowSize = 40;
    private const double VisibleWidth = 1;
    private const double PulseSize = 4;
    private const double PulseMinimumOpacity = 0.2;
    private const PRINT_WINDOW_FLAGS RenderFullContent = (PRINT_WINDOW_FLAGS)2;

    private static readonly TimeSpan DrawTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan PulseDuration = TimeSpan.FromMilliseconds(500);

    private readonly Window _window;
    private readonly HWND _handle;

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
        _handle = new HWND(new WindowInteropHelper(_window).Handle);
    }

    /// <summary>
    ///     Shows the window and waits until it shows the color.
    /// </summary>
    /// <param name="color">The color of the window.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the wait.</param>
    /// <returns>
    ///     A task that represents the asynchronous show operation.
    ///     The task result contains the shown window.
    /// </returns>
    /// <exception cref="InvalidOperationException">The window shows another color when the time to draw a frame runs out.</exception>
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

    /// <summary>
    ///     Fills the window with the color.
    /// </summary>
    /// <param name="color">The color of the window.</param>
    public void Fill(Color color)
    {
        _window.Background = new SolidColorBrush(color);
    }

    /// <summary>
    ///     Waits until the window shows the expected color, or until the time to draw a frame runs out.
    /// </summary>
    /// <param name="expectedColor">The color to wait for.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> used to cancel the wait.</param>
    /// <returns>
    ///     A task that represents the asynchronous wait operation.
    ///     The task result contains the color the window shows.
    /// </returns>
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

    /// <inheritdoc />
    public void Dispose()
    {
        _window.Close();
    }

    /// <summary>
    ///     Reads the center pixel of the frame Windows composes for the window.
    /// </summary>
    /// <returns>The color of the center pixel.</returns>
    private Color ReadDrawnColor()
    {
        TestPInvoke.GetClientRect(_handle, out var bounds);
        using var bitmap = new Bitmap(bounds.right, bounds.bottom);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            var context = graphics.GetHdc();
            try
            {
                TestPInvoke.PrintWindow(_handle, new HDC(context), RenderFullContent);
            }
            finally
            {
                graphics.ReleaseHdc(context);
            }
        }

        var pixel = bitmap.GetPixel(bounds.right / 2, bounds.bottom / 2);
        return Color.FromRgb(pixel.R, pixel.G, pixel.B);
    }
}
