using Windows.Win32;
using Windows.Win32.Foundation;

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Provides access to the window WPF creates on each UI thread to receive the notifications of its render thread.
/// </summary>
/// <remarks>
///     The window title and the registered message name are internal to WPF and have the same values in .NET Framework and .NET.
/// </remarks>
internal static class MediaContextNotificationWindow
{
    private const string WindowName = "MediaContextNotificationWindow";
    private static readonly uint ChannelNotifyMessage = PInvoke.RegisterWindowMessage("MilChannelNotify");

    [ThreadStatic] private static HWND _window;

    /// <summary>
    ///     Posts the channel notification to the notification window of the current thread.
    /// </summary>
    /// <remarks>
    ///     The method caches the window handle per thread after the first search.
    ///     A thread without a WPF dispatcher has no notification window, and the method posts nothing.
    /// </remarks>
    public static void PostChannelNotification()
    {
        if (!_window.IsNull && PInvoke.PostMessage(_window, ChannelNotifyMessage, default, default))
        {
            return;
        }

        _window = FindThreadWindow();
        if (!_window.IsNull)
        {
            PInvoke.PostMessage(_window, ChannelNotifyMessage, default, default);
        }
    }

    /// <summary>
    ///     Finds the notification window of the current thread among the top-level windows of the desktop.
    /// </summary>
    /// <returns>The window handle, or a null handle when the thread has no notification window.</returns>
    private static unsafe HWND FindThreadWindow()
    {
        var threadId = PInvoke.GetCurrentThreadId();
        var window = HWND.Null;
        while (true)
        {
            window = PInvoke.FindWindowEx(HWND.Null, window, null, WindowName);
            if (window.IsNull || PInvoke.GetWindowThreadProcessId(window, null) == threadId)
            {
                return window;
            }
        }
    }
}
