// ReSharper disable once CheckNamespace
namespace Nice3point.Revit.Toolkit.External;

public interface IAsyncExternalEvent<T>
{
    Task<T> RaiseAsync();
}