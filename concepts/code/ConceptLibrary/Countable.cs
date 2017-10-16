using System.Concepts.Enumerable;

namespace System.Concepts.Countable
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
    public concept CCountable<TEnum>
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
        int Count(this TEnum t);
    }

    /// <summary>
    /// Naive destructive instance of <see cref="CCountable{TEnum}"/> for
    /// enumerators, where we just count up the number of remaining items.
    /// </summary>
    [Overlappable]
    public instance Countable_Enumerator<TEnum, [AssociatedType]TElem, implicit E> : CCountable<TEnum> where E : CEnumerator<TEnum, TElem>
    {
        int Count(this TEnum t)
        {
            // @MattWindsor91
            // We used to reset here, but not all enumerators implement Reset.
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
    public concept CStaticCountable<TEnum> : CCountable<TEnum>
    {
        // This concept adds only additional semantic guarantees to CCountable.
    }

    /// <summary>
    /// Instance for O(1) length lookup of arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance StaticCountable_Array<TElem> : CStaticCountable<TElem[]>
    {
        int Count(this TElem[] t) => t.Length;
    }

    /// <summary>
    /// Instance for O(1) length lookup of array cursors.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance StaticCountable_ArrayCursor<TElem> : CStaticCountable<Instances.ArrayCursor<TElem>>
    {
        int Count(this Instances.ArrayCursor<TElem> t) => t.hi;
    }

    /// <summary>
    /// Instance for O(1) length lookup of range cursors.
    /// </summary>
    /// <typeparam name="TNum">
    /// Type of the number in the range.
    /// </typeparam>
    public instance StaticCountable_RangeCursor<TNum> : CStaticCountable<Instances.RangeCursor<TNum>>
    {
        int Count(this Instances.RangeCursor<TNum> t) => t.range.count;
    }
}
