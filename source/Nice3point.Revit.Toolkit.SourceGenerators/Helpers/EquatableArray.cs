using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     Extensions for <see cref="EquatableArray{T}" />.
/// </summary>
internal static class EquatableArray
{
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> instance.</param>
    /// <typeparam name="T">The type of items in the input array.</typeparam>
    extension<T>(ImmutableArray<T> array) where T : IEquatable<T>
    {
        /// <summary>
        ///     Creates an <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray{T}" />.
        /// </summary>
        /// <returns>An <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray{T}" />.</returns>
        public EquatableArray<T> AsEquatableArray()
        {
#if NET
            return [with(array)];
#else
            return new EquatableArray<T>(array);
#endif
        }
    }
}

/// <summary>
///     A wrapper around arrays that provides value equality semantics.
///     Essential for incremental source generators where proper caching depends on equality comparison.
/// </summary>
/// <typeparam name="T">The element type, which must implement IEquatable&lt;T&gt;</typeparam>
/// <summary>
///     An immutable, equatable array. This is equivalent to <see cref="ImmutableArray" /> but with value equality support.
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>
    ///     The underlying <typeparamref name="T" /> array.
    /// </summary>
    private readonly T[]? _array;

    /// <summary>
    ///     Creates a new <see cref="EquatableArray{T}" /> instance.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> to wrap.</param>
    public EquatableArray(ImmutableArray<T> array)
    {
        _array = Unsafe.As<ImmutableArray<T>, T[]?>(ref array);
    }

    /// <summary>
    ///     Gets a reference to an item at a specified position within the array.
    /// </summary>
    /// <param name="index">The index of the item to retrieve a reference to.</param>
    /// <returns>A reference to an item at a specified position within the array.</returns>
    public ref readonly T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref AsImmutableArray().ItemRef(index);
    }

    /// <summary>
    ///     Gets a value indicating whether the current array is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AsImmutableArray().IsEmpty;
    }

    /// <summary>
    ///     Gets the current array length.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => AsImmutableArray().Length;
    }

    /// <sinheritdoc />
    public bool Equals(EquatableArray<T> array)
    {
        return AsSpan().SequenceEqual(array.AsSpan());
    }

    /// <sinheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is EquatableArray<T> array && Equals(array);
    }

    /// <sinheritdoc />
    public override int GetHashCode()
    {
        if (_array is not { } array)
        {
            return 0;
        }

        HashCode hashCode = default;

        foreach (var item in array)
        {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Gets an <see cref="ImmutableArray{T}" /> instance from the current <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <returns>The <see cref="ImmutableArray{T}" /> from the current <see cref="EquatableArray{T}" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<T> AsImmutableArray()
    {
        return Unsafe.As<T[]?, ImmutableArray<T>>(ref Unsafe.AsRef(in _array));
    }

    /// <summary>
    ///     Creates an <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray{T}" /> instance.</param>
    /// <returns>An <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray{T}" />.</returns>
    public static EquatableArray<T> FromImmutableArray(ImmutableArray<T> array)
    {
#if NET
            return [with(array)];
#else
        return new EquatableArray<T>(array);
#endif
    }

    /// <summary>
    ///     Returns a <see cref="ReadOnlySpan{T}" /> wrapping the current items.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}" /> wrapping the current items.</returns>
    public ReadOnlySpan<T> AsSpan()
    {
        return AsImmutableArray().AsSpan();
    }

    /// <summary>
    ///     Copies the contents of this <see cref="EquatableArray{T}" /> instance to a mutable array.
    /// </summary>
    /// <returns>The newly instantiated array.</returns>
    public T[] ToArray()
    {
        return AsImmutableArray().ToArray();
    }

    /// <summary>
    ///     Gets an <see cref="ImmutableArray{T}.Enumerator" /> value to traverse items in the current array.
    /// </summary>
    /// <returns>An <see cref="ImmutableArray{T}.Enumerator" /> value to traverse items in the current array.</returns>
    public ImmutableArray<T>.Enumerator GetEnumerator()
    {
        return AsImmutableArray().GetEnumerator();
    }

    /// <sinheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
    }

    /// <sinheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)AsImmutableArray()).GetEnumerator();
    }

    /// <summary>
    ///     Implicitly converts an <see cref="ImmutableArray{T}" /> to <see cref="EquatableArray{T}" />.
    /// </summary>
    /// <returns>An <see cref="EquatableArray{T}" /> instance from a given <see cref="ImmutableArray{T}" />.</returns>
    public static implicit operator EquatableArray<T>(ImmutableArray<T> array)
    {
        return FromImmutableArray(array);
    }

    /// <summary>
    ///     Implicitly converts an <see cref="EquatableArray{T}" /> to <see cref="ImmutableArray{T}" />.
    /// </summary>
    /// <returns>An <see cref="ImmutableArray{T}" /> instance from a given <see cref="EquatableArray{T}" />.</returns>
    public static implicit operator ImmutableArray<T>(EquatableArray<T> array)
    {
        return array.AsImmutableArray();
    }

    /// <summary>
    ///     Checks whether two <see cref="EquatableArray{T}" /> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> value.</param>
    /// <returns>Whether <paramref name="left" /> and <paramref name="right" /> are equal.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Checks whether two <see cref="EquatableArray{T}" /> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}" /> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}" /> value.</param>
    /// <returns>Whether <paramref name="left" /> and <paramref name="right" /> are not equal.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }
}
