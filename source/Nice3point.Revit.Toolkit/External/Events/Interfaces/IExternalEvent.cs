using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///     An interface for raising an external event to execute a handler within the Revit API context.
/// </summary>
[PublicAPI]
public interface IExternalEvent
{
    /// <summary>
    ///     Raises (signals) the external event, instructing Revit to execute the handler within the Revit API context.
    /// </summary>
    void Raise();
}