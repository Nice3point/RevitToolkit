using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

/// <summary>
/// Options to customize the behavior of <see cref="IExternalEvent"/> instances.
/// </summary>
[Flags]
[PublicAPI]
public enum ExternalEventOptions
{
    /// <summary>
    /// No option is specified. The handler will always be queued via <see cref="IExternalEvent.Raise"/>,
    /// regardless of whether the call is made from within the Revit API context.
    /// </summary>
    None = 0,

    /// <summary>
    /// When the handler is raised from within the Revit API context (i.e. <c>RevitContext.IsRevitInApiMode</c> is <see langword="true"/>),
    /// it will be invoked directly on the calling thread instead of being queued via <see cref="IExternalEvent.Raise"/>.
    /// </summary>
    AllowDirectInvocation = 1 << 0
}