using Autodesk.Revit.DB;

namespace Nice3point.Revit.Toolkit.Utils;

public static class CollectorManager
{
    public static ICollection<ElementId> GetInstancesIdsByCategory(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .ToElementIds();
    }

    public static IList<Element> GetInstancesByCategory(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .ToElements();
    }

    public static ICollection<ElementId> GetTypesIdsByCategory(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .ToElementIds();
    }

    public static IList<Element> GetTypesByCategory(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .ToElements();
    }
}