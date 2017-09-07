using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>
    /// Concept for enumerators that can be counted.
    /// <para>
    /// The count may force evaluation of the enumerator's value.
    /// For guarantees that this will not happen, see
    /// the descendent <see cref="CStaticCount{TEnum}"/></para>
    /// </summary>
    /// <typeparam name="TEnum">
    /// The type of the enumerator.
    /// </typeparam>
    public concept CCount<TEnum>
    {
        /// <summary>
        /// Returns the number of elements over which this enumerator ranges.
        /// <para>
        /// This may cause evaluation of elements in the enumerator and move
        /// the iterator, and need not be thread-safe.
        /// </para>
        /// </summary>
        /// <param name="t">The enumerator to count.</param>
        /// <returns>
        /// The total number of elements accessible from this enumerator,
        /// including any previously moved-over.
        /// </returns>
        int Count(ref TEnum t);
    }

    /// <summary>
    /// Naive instance of <see cref="CCount{TEnum}"/> for enumerators,
    /// where we just reset and spin the enumerator.
    /// </summary>
    [Overlappable]
    public instance Count_Enumerator<TEnum, [AssociatedType]TElem, implicit E> : CCount<TEnum> where E : CEnumerator<TEnum, TElem>
    {
        int Count(ref TEnum t)
        {
            E.Reset(ref t);
            var count = 0;
            while (E.MoveNext(ref t))
            {
                count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Concept for enumerators that can be counted without enumerating.
    /// <para>
    /// Instances for this concept must guarantee that the enumerator is not
    /// modified at any point.</para>
    /// </summary>
    /// <typeparam name="TEnum">
    /// The type of the enumerator.
    /// </typeparam>
    public concept CStaticCount<TEnum> : CCount<TEnum>
    {
        // This concept adds only additional semantic guarantees to CCount.
    }

    /// <summary>
    /// Instance for O(1) length lookup of arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance StaticCount_Array<TElem> : CStaticCount<TElem[]>
    {
        int Count(ref TElem[] t) => t.Length;
    }

    /// <summary>
    /// Instance for O(1) length lookup of array cursors.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance StaticCount_ArrayCursor<TElem> : CStaticCount<Instances.ArrayCursor<TElem>>
    {
        int Count(ref Instances.ArrayCursor<TElem> t) => t.hi;
    }

    /// <summary>
    /// Instance for O(1) length lookup of selections, when the selected-over
    /// collection is itself bounded.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the source of the selection.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the elements of <typeparamref name="TEnum"/>.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of the projected elements of the selection.
    /// </typeparam>
    /// <typeparam name="B">
    /// Instance of <see cref="CStaticCount{T}"/> for <typeparamref name="TEnum"/>.
    /// </typeparam>
    public instance CBounded_Select<TEnum, TElem, TProj, implicit S> : CStaticCount<Select<TEnum, TElem, TProj>>
        where S : CStaticCount<TEnum>
    {
        int Count(ref Select<TEnum, TElem, TProj> sel) => S.Count(ref sel.source);
    }
}
