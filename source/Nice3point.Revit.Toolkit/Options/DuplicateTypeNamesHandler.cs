using System.ComponentModel;

namespace Nice3point.Revit.Toolkit.Options;

/// <summary>
///     A class for custom handlers of duplicate type names encountered during a paste operation. When the destination document
///     contains types that have the same names as the types being copied, but different internals, a decision must be made on how to proceed - whether to
///     cancel the operation or continue, but only copy types with unique names.
/// </summary>
[PublicAPI]
public class DuplicateTypeNamesHandler : IDuplicateTypeNamesHandler
{
    private readonly DuplicateTypeAction _duplicateTypeAction;
    private DuplicateTypeNamesHandlerArgs _duplicateArguments;

    /// <summary>
    ///     Creates a new handler with <see cref="DuplicateTypeAction.UseDestinationTypes" /> by default
    /// </summary>
    public DuplicateTypeNamesHandler()
    {
        _duplicateTypeAction = DuplicateTypeAction.UseDestinationTypes;
    }

    /// <summary>
    ///     Creates a new handler
    /// </summary>
    /// <param name="action">A structure that provides information about an attempt to copy types with names that already exist in the destination document</param>
    public DuplicateTypeNamesHandler(DuplicateTypeAction action)
    {
        _duplicateTypeAction = action;
    }

    /// <summary>
    ///     Creates a new handler
    /// </summary>
    /// <param name="actionHandler">
    ///     Encapsulates a method on a structure that provides information about an attempt to copy types with names that already exist in the destination document
    /// </param>
    public DuplicateTypeNamesHandler(Func<DuplicateTypeNamesHandlerArgs, DuplicateTypeAction> actionHandler)
    {
        _duplicateTypeAction = actionHandler(_duplicateArguments);
    }

    /// <summary>
    ///     Called when the destination document contains types with the same names as the types being copied
    /// </summary>
    /// <param name="args">The information about the types with duplicate names</param>
    /// <returns>
    ///     The action to be taken: copy only types with unique names or cancel the operation
    /// </returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DuplicateTypeAction OnDuplicateTypeNamesFound(DuplicateTypeNamesHandlerArgs args)
    {
        _duplicateArguments = args;
        return _duplicateTypeAction;
    }
}