using Autodesk.Revit.DB;

namespace Nice3point.Revit.Toolkit.Utils;

public static class CollectorUtils
{
    public static IList<Element> GetInstances(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .ToElements();
    }

    public static IEnumerable<Element> EnumerateInstances(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category);
    }

    public static IEnumerable<T> EnumerateInstances<T>(Document document, BuiltInCategory category) where T : Element
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .OfClass(typeof(T))
            .Cast<T>();
    }

    public static ICollection<ElementId> GetInstancesIds(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .ToElementIds();
    }

    public static IEnumerable<ElementId> EnumerateInstancesIds(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .Select(element => element.Id);
    }

    public static IEnumerable<ElementId> EnumerateInstancesIds<T>(Document document, BuiltInCategory category) where T : Element
    {
        return new FilteredElementCollector(document)
            .WhereElementIsNotElementType()
            .OfCategory(category)
            .OfClass(typeof(T))
            .Cast<T>()
            .Select(element => element.Id);
    }

    public static IList<Element> GetTypes(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .ToElements();
    }

    public static IEnumerable<Element> EnumerateTypes(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category);
    }

    public static IEnumerable<T> EnumerateTypes<T>(Document document, BuiltInCategory category) where T : ElementType
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .OfClass(typeof(T))
            .Cast<T>();
    }

    public static ICollection<ElementId> GetTypesIds(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .ToElementIds();
    }

    public static IEnumerable<ElementId> EnumerateTypesIds(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .Select(element => element.Id);
    }

    public static IEnumerable<ElementId> EnumerateTypesIds<T>(Document document, BuiltInCategory category) where T : ElementType
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .OfClass(typeof(T))
            .Cast<T>()
            .Select(element => element.Id);
    }

    public static Category GetCategory(Document document, BuiltInCategory category)
    {
        return new FilteredElementCollector(document)
            .WhereElementIsElementType()
            .OfCategory(category)
            .FirstElement()
            .Category;
    }
}