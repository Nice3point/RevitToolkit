using System.ComponentModel;
using System.Reflection;
using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions.UI;
using Nice3point.Revit.Toolkit.External;

namespace Nice3point.Revit.Toolkit.Ui.Tests;

[UsedImplicitly]
public class Application : ExternalApplication
{
    private const string DefaultTestGroup = "General";

    public override void OnStartup()
    {
        var panel = Application.CreatePanel("Toolkit Tests", "Nice3point.Revit.Toolkit");
        RegisterTestCommands(panel);
    }

    private static void RegisterTestCommands(RibbonPanel panel)
    {
        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.IsAssignableTo<IExternalCommand>())
            .ToList();

        var groupedCommands = commandTypes
            .GroupBy(GetGroupName)
            .OrderBy(group => group.Key == DefaultTestGroup ? 1 : 0)
            .ThenBy(group => group.Key);

        foreach (var group in groupedCommands)
        {
            var pullButton = panel.AddPullDownButton(group.Key, group.Key);
            pullButton.SetImage("/Nice3point.Revit.Toolkit.Ui.Tests;component/Resources/Icons/RibbonIcon16.png");
            pullButton.SetLargeImage("/Nice3point.Revit.Toolkit.Ui.Tests;component/Resources/Icons/RibbonIcon32.png");

            foreach (var commandType in group.OrderBy(type => type.Name))
            {
                pullButton.AddPushButton(commandType, commandType.Name);
            }
        }
    }

    private static string GetGroupName(Type type)
    {
        var displayNameAttribute = type.GetCustomAttribute<DisplayNameAttribute>();
        return displayNameAttribute?.DisplayName ?? DefaultTestGroup;
    }
}