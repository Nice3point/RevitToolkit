using System.Collections.Immutable;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Tests.Helpers;

public sealed class EquatableArrayTests
{
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(3)]
    public async Task EqualSequences_HaveEqualValuesAndHashCodesAsync(int length)
    {
        var first = new EquatableArray<int>(Enumerable.Range(1, length).ToImmutableArray());
        var second = new EquatableArray<int>(Enumerable.Range(1, length).ToImmutableArray());

        await Assert.That(first.Equals(second)).IsTrue();
        await Assert.That(first == second).IsTrue();
        await Assert.That(first != second).IsFalse();
        await Assert.That(first.GetHashCode()).IsEqualTo(second.GetHashCode());
    }

    [Test]
    [Arguments(new[] { 2, 1, 3 })]
    [Arguments(new[] { 1, 2, 4 })]
    [Arguments(new[] { 1, 2 })]
    public async Task DifferentSequences_AreNotEqualAsync(int[] values)
    {
        var first = new EquatableArray<int>(ImmutableArray.Create(1, 2, 3));
        var second = new EquatableArray<int>(values.ToImmutableArray());

        await Assert.That(first.Equals(second)).IsFalse();
        await Assert.That(first == second).IsFalse();
        await Assert.That(first != second).IsTrue();
    }

    [Test]
    public async Task ConversionAndEnumeration_PreserveSequenceAsync()
    {
        var source = ImmutableArray.Create(3, 7, 11);
        EquatableArray<int> array = source;
        ImmutableArray<int> converted = array;

        await Assert.That(array.Length).IsEqualTo(3);
        await Assert.That(array.IsEmpty).IsFalse();
        await Assert.That(array[1]).IsEqualTo(7);
        await Assert.That(converted.SequenceEqual(source)).IsTrue();
        await Assert.That(array.SequenceEqual(source)).IsTrue();
        await Assert.That(array.AsSpan().SequenceEqual(source.AsSpan())).IsTrue();

        var copy = array.ToArray();
        copy[1] = 99;

        await Assert.That(array[1]).IsEqualTo(7);
    }

    [Test]
    [Category("Regression")]
    public async Task DefaultAndEmpty_HaveEqualHashCodesAsync()
    {
        var defaultArray = default(EquatableArray<int>);
        var emptyArray = new EquatableArray<int>(ImmutableArray<int>.Empty);

        await Assert.That(defaultArray.Equals(emptyArray)).IsTrue();
        await Assert.That(defaultArray.GetHashCode()).IsEqualTo(emptyArray.GetHashCode());
    }
}
