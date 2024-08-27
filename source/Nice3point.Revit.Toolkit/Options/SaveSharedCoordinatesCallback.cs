namespace Nice3point.Revit.Toolkit.Options;

/// <summary>
///     Callback that may be used to control Revit when trying to unload or reload a Revit link with changes in shared coordinates.
/// </summary>
[PublicAPI]
public class SaveSharedCoordinatesCallback : ISaveSharedCoordinatesCallback
{
    private readonly Func<RevitLinkType, SaveModifiedLinksOptions>? _handler;
    private readonly SaveModifiedLinksOptions _options;

    /// <summary>
    ///     Creates a new callback with <see cref="SaveModifiedLinksOptions.SaveLinks" /> by default.
    /// </summary>
    public SaveSharedCoordinatesCallback()
    {
        _options = SaveModifiedLinksOptions.SaveLinks;
    }

    /// <summary>
    ///     Creates a new callback
    /// </summary>
    /// <param name="options">The options when saving a linked file which has been modified in-memory by shared coordinates operations.</param>
    public SaveSharedCoordinatesCallback(SaveModifiedLinksOptions options)
    {
        _options = options;
    }

    /// <summary>
    ///     Creates a new callback
    /// </summary>
    /// <param name="handler">Encapsulates a method for options when saving a linked file which has been modified in-memory by shared coordinates operations.</param>
    public SaveSharedCoordinatesCallback(Func<RevitLinkType, SaveModifiedLinksOptions> handler)
    {
        _handler = handler;
    }

    /// <summary>
    ///     Determines whether Revit should save the link, not save the link, or discard shared positioning entirely.
    /// </summary>
    /// <param name="link">The Revit link which has modified shared coordinates.</param>
    /// <returns>
    ///     The options when saving a linked file which has been modified in-memory by shared coordinates operations.
    /// </returns>
    public SaveModifiedLinksOptions GetSaveModifiedLinksOption(RevitLinkType link)
    {
        if (_handler is not null) return _handler.Invoke(link);
        return _options;
    }
}