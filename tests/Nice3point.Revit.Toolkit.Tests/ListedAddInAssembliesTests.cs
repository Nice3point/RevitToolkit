using System.Reflection;
using Nice3point.Revit.Toolkit.Internal;
using Nice3point.TUnit.Revit;

namespace Nice3point.Revit.Toolkit.Tests;

public sealed class ListedAddInAssembliesTests : RevitApiUiTest
{
    private const nint FirstAddIn = 1;
    private const nint SecondAddIn = 2;
    private static readonly Assembly ListedAssembly = typeof(ListedAddInAssembliesTests).Assembly;
    private static readonly string ListedName = ListedAssembly.GetName().Name!;

    [Test]
    public async Task TryMatch_ListedName_ReturnsTheAddIn()
    {
        // Arrange
        var listedAssemblies = new ListedAddInAssemblies();
        listedAssemblies.Add([ListedName], FirstAddIn, replace: false);

        // Act
        var isMatched = listedAssemblies.TryMatch(ListedAssembly, out var addInData);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isMatched).IsTrue();
            await Assert.That(addInData).IsEqualTo(FirstAddIn);
        }
    }

    [Test]
    public async Task TryMatch_ListedNameInAnotherCase_ReturnsTheAddIn()
    {
        // Arrange
        var listedAssemblies = new ListedAddInAssemblies();
        listedAssemblies.Add([ListedName.ToUpperInvariant()], FirstAddIn, replace: false);

        // Act
        var isMatched = listedAssemblies.TryMatch(ListedAssembly, out var addInData);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isMatched).IsTrue().Because("the runtime compares simple assembly names case-insensitively");
            await Assert.That(addInData).IsEqualTo(FirstAddIn);
        }
    }

    [Test]
    public async Task TryMatch_UnlistedName_ReturnsFalse()
    {
        // Arrange
        var listedAssemblies = new ListedAddInAssemblies();
        listedAssemblies.Add(["MyAddIn.Core"], FirstAddIn, replace: false);

        // Act
        var isMatched = listedAssemblies.TryMatch(ListedAssembly, out var addInData);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isMatched).IsFalse();
            await Assert.That(addInData).IsEqualTo(0);
        }
    }

    [Test]
    public async Task TryMatch_NameListedAfterAMiss_ReturnsTheAddIn()
    {
        // Arrange
        var listedAssemblies = new ListedAddInAssemblies();
        listedAssemblies.Add(["MyAddIn.Core"], FirstAddIn, replace: false);
        var isMatchedBeforeListing = listedAssemblies.TryMatch(ListedAssembly, out _);
        listedAssemblies.Add([ListedName], SecondAddIn, replace: false);

        // Act
        var isMatched = listedAssemblies.TryMatch(ListedAssembly, out var addInData);

        // Assert
        using (Assert.Multiple())
        {
            await Assert.That(isMatchedBeforeListing).IsFalse();
            await Assert.That(isMatched).IsTrue().Because("a new listed name invalidates the cached miss");
            await Assert.That(addInData).IsEqualTo(SecondAddIn);
        }
    }

    [Test]
    public async Task Add_NameListedEarlierWithoutReplace_KeepsTheFirstAddIn()
    {
        // Arrange
        var listedAssemblies = new ListedAddInAssemblies();
        listedAssemblies.Add([ListedName], FirstAddIn, replace: false);

        // Act
        listedAssemblies.Add([ListedName], SecondAddIn, replace: false);

        // Assert
        listedAssemblies.TryMatch(ListedAssembly, out var addInData);
        await Assert.That(addInData).IsEqualTo(FirstAddIn);
    }

    [Test]
    public async Task Add_NameListedEarlierWithReplace_ReplacesTheAddIn()
    {
        // Arrange
        var listedAssemblies = new ListedAddInAssemblies();
        listedAssemblies.Add([ListedName], FirstAddIn, replace: false);

        // Act
        listedAssemblies.Add([ListedName], SecondAddIn, replace: true);

        // Assert
        listedAssemblies.TryMatch(ListedAssembly, out var addInData);
        await Assert.That(addInData).IsEqualTo(SecondAddIn);
    }
}
