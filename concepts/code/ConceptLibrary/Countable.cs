using System.Concepts.Enumerable;

namespace System.Concepts.Countable
{
    /// <summary>
    /// Concept for collections that can be counted.
    /// <para>
    /// The count may force evaluation of the enumerator's value.
    /// For guarantees that this will not happen, see
    /// the descendent <see cref="CStaticCountable{TColl}"/></para>
    /// </summary>
    /// <typeparam name="TColl">
    /// The type of the collection.
    /// </typeparam>
    public concept CCountable<TColl>
    {
        /// <summary>
        /// Returns the number of elements over which this collection ranges.
        /// </summary>
        /// <param name="collection">The collection to count.</param>
        /// <returns>
        /// The total number of elements in this collection.
        /// </returns>
        int Count(this TColl collection);
    }

    /// <summary>
    /// Enumerables are always countable, by running them to completion.
    /// </summary>
    [Overlappable]
    public instance Countable_Enumerable<TColl, [AssociatedType]TEnum, [AssociatedType]TElem, implicit Eb, implicit Et>
        : CCountable<TColl>
        where Eb : CEnumerable<TColl, TEnum>
        where Et : CEnumerator<TEnum, TElem>
    {
        int Count(this TColl collection)
        {
            var count = 0;
            var e = collection.GetEnumerator();
            while (Et.MoveNext(ref e))
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
}
