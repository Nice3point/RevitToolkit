using System.Reflection;
using System.Runtime.CompilerServices;

namespace Nice3point.Revit.Toolkit.Internal;

/// <summary>
///     Maps the assembly names listed by <see cref="External.AddInAssemblyAttribute" /> to the add-in of the listing assembly.
/// </summary>
/// <remarks>
///     Assembly names are compared case-insensitively, as the runtime compares simple names.
///     The type is not thread-safe.
/// </remarks>
internal sealed class ListedAddInAssemblies
{
    private readonly Dictionary<string, nint> _addIns = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConditionalWeakTable<Assembly, StrongBox<int>> _unmatchedAssemblies = new();

    /// <summary>
    ///     Lists the assembly names under the add-in.
    /// </summary>
    /// <param name="assemblyNames">The simple names of the assemblies.</param>
    /// <param name="addInData">The pointer to the add-in data.</param>
    /// <param name="replace"><see langword="true" /> to overwrite a name listed earlier; otherwise, <see langword="false" />.</param>
    public void Add(IEnumerable<string> assemblyNames, nint addInData, bool replace)
    {
        foreach (var assemblyName in assemblyNames)
        {
            if (replace || !_addIns.ContainsKey(assemblyName))
            {
                _addIns[assemblyName] = addInData;
            }
        }
    }

    /// <summary>
    ///     Finds the add-in listed for the name of the assembly.
    /// </summary>
    /// <param name="assembly">The assembly to match.</param>
    /// <param name="addInData">When this method returns, contains the pointer to the add-in data, if found.</param>
    /// <returns><see langword="true" /> if the name of the assembly is listed; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    ///     A miss is cached until a call to <see cref="Add" /> lists a new name.
    /// </remarks>
    public bool TryMatch(Assembly assembly, out nint addInData)
    {
        addInData = 0;
        if (_unmatchedAssemblies.TryGetValue(assembly, out var listedNameCount) && listedNameCount.Value == _addIns.Count)
        {
            return false;
        }

        var assemblyName = assembly.GetName().Name;
        if (assemblyName is not null && _addIns.TryGetValue(assemblyName, out addInData))
        {
            return true;
        }

        _unmatchedAssemblies.Remove(assembly);
        _unmatchedAssemblies.Add(assembly, new StrongBox<int>(_addIns.Count));
        return false;
    }
}
