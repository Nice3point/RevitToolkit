using System.Reflection;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.External;

/// <summary>
///    Implementation for a Revit add-in External Command
/// </summary>
public abstract class ExternalCommand : IExternalCommand
{
    /// <summary>
    ///     Element set indicating problem elements to display in the failure dialog. This will be used only if the command status was "Failed".
    /// </summary>
    public ElementSet ElementSet { get; private set; }

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.ExternalCommandData"/> that is needed by an external command
    /// </summary>
    public ExternalCommandData ExternalCommandData { get; private set; }

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIApplication"/> that is needed by an external command
    /// </summary>
    public UIApplication UiApplication { get; set; }

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.UI.UIDocument"/> that is needed by an external command
    /// </summary>
    public UIDocument UiDocument => UiApplication.ActiveUIDocument;

    /// <summary>
    ///     Reference to the <see cref="Autodesk.Revit.DB.Document"/> that is needed by an external command
    /// </summary>
    public Document Document => UiApplication.ActiveUIDocument.Document;

    /// <summary>
    ///     External Command Exception Handler
    /// </summary>
    /// <remarks>If no handler is set, exceptions will be handled by Revit</remarks>
    public Action<ExternalCommand, Exception> ExceptionHandler { get; set; }

    /// <summary>
    ///     Informs Autodesk Revit of the status of your application after execution.
    /// </summary>
    /// <remarks>
    ///     The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
    /// succeed, Revit will undo any changes made by the external command
    /// </remarks>
    public Result Result { get; set; } = Result.Succeeded;

    /// <summary>
    ///     Error message can be returned by external command. This will be displayed only if the command status was "Failed" <br/>
    ///     There is a limit of 1023 characters for this message; strings longer than this will be truncated.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>Override this method to implement and external command within Revit.</summary>
    /// <returns>
    ///     The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
    ///     succeed, Revit will undo any changes made by the external command.
    /// </returns>
    /// <param name="commandData">
    ///     An ExternalCommandData object which contains reference to Application and View
    ///     needed by external command.
    /// </param>
    /// <param name="message">
    ///     Error message can be returned by external command. This will be displayed only if the command status
    ///     was "Failed".  There is a limit of 1023 characters for this message; strings longer than this will be truncated.
    /// </param>
    /// <param name="elements">
    ///     Element set indicating problem elements to display in the failure dialog.  This will be used
    ///     only if the command status was "Failed".
    /// </param>
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        ElementSet = elements;
        ExternalCommandData = commandData;
        UiApplication = commandData.Application;

        try
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            Execute();
        }
        catch (Exception ex)
        {
            if (ExceptionHandler is null) throw;
            else ExceptionHandler(this, ex);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        message = ErrorMessage;
        return Result;
    }

    /// <summary>
    ///     Overload this method to implement and external command within Revit
    /// </summary>
    public virtual void Execute()
    {
    }

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var assemblies = Directory.EnumerateFiles(currentDirectory, "*.dll");
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var assembly in assemblies)
            if (args.Name.Contains(Path.GetFileNameWithoutExtension(assembly)))
                return Assembly.LoadFile(assembly);

        return null;
    }
}