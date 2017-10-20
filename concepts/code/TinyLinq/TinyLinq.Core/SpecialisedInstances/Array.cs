using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;

namespace TinyLinq.SpecialisedInstances
{
    // Specialised instances for executing queries on arrays.
    //
    // These reduce various query patterns into a single struct.

    #region Select

    /// <summary>Specialised cursor for Select queries on arrays.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct ArraySelectCursor<TSource, TResult>
    {
        /// <summary>The selector function.</summary>
        public readonly Func<TSource, TResult> selector;
        /// <summary>The source array.</summary>
        public readonly TSource[] source;
        /// <summary>The cached length of the source array.</summary>
        public readonly int sourceLength;

        /// <summary>The current source index,  May be +-1 bounds.</summary>
        public int sourceIndex;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>Creates a new array select cursor.</summary>
        /// <param name="source">The source array of the select.</param>
        /// <param name="selector">The selector function.</param>
        public ArraySelectCursor(TSource[] source, Func<TSource, TResult> selector)
        {
            this.selector = selector;
            this.source = source;
            sourceLength = source.Length;

            sourceIndex = -1;
            result = default;
        }
    }

    /// <summary>Array Selects are cloneable enumerators.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Enumerator_ArraySelectCursor<TSource, TResult> :
        CCloneableEnumerator<ArraySelectCursor<TSource, TResult>, TResult>
    {
        ArraySelectCursor<TSource, TResult> Clone(ref this ArraySelectCursor<TSource, TResult> c) =>
            new ArraySelectCursor<TSource, TResult>(c.source, c.selector);

        void Reset(ref ArraySelectCursor<TSource, TResult> c)
        {
            c.sourceIndex = -1;
        }

        bool MoveNext(ref ArraySelectCursor<TSource, TResult> c)
        {
            // Are we already on the last element?
            if (c.sourceIndex < c.sourceLength)
            {
                return false;
            }

            c.sourceIndex++;
            c.result = c.selector(c.source[c.sourceIndex]);
            return true;
        }

        TResult Current(ref ArraySelectCursor<TSource, TResult> c) => c.result;

        void Dispose(ref ArraySelectCursor<TSource, TResult> c) { }
    }


    /// <summary>Array Selects are countable.</summary>
    /// <typeparam name="TSource">Type of elements in the array.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>

    public instance Countable_ArraySelectCursor<TSource, TResult> :
        CCountable<ArraySelectCursor<TSource, TResult>>
    {
        int Count(this ArraySelectCursor<TSource, TResult> c)
        {
            // The selector might be impure, so we must run it for each element
            foreach (var s in c.source)
            {
                c.selector(s);
            }

            return c.sourceLength;
        }
    }

    /// <summary>Specialised instance for Select queries on arrays.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Select_Array<TSource, TResult> : CSelect<TSource[], TSource, TResult, ArraySelectCursor<TSource, TResult>>
    {
        ArraySelectCursor<TSource, TResult> Select(this TSource[] source, Func<TSource, TResult> selector) =>
            new ArraySelectCursor<TSource, TResult>(source, selector);
    }

    #endregion Select

    #region SelectMany


    /// <summary>
    /// Specialised cursor for SelectMany queries where both
    /// the source and the inner collection are arrays.
    /// </summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TCollection">Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct ArrayToArraySelectManyCursor<TSource, TCollection, TResult>
    {
        /// <summary>The collection selector function.</summary>
        public readonly Func<TSource, TCollection[]> collectionSelector;
        /// <summary>The result selector function.</summary>
        public readonly Func<TSource, TCollection, TResult> resultSelector;
        /// <summary>The source array.</summary>
        public readonly TSource[] source;
        /// <summary>The cached length of the source array.</summary>
        public readonly int sourceLength;

        /// <summary>The current source index, may be +-1 bounds.</summary>
        public int sourceIndex;
        /// <summary>The current collection array.</summary>
        public TCollection[] collection;
        /// <summary>The current collection index, may be +-1 bounds.</summary>
        public int collectionIndex;
        /// <summary>The cached length of the collection array.</summary>
        public int collectionLength;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>
        /// Creates a new array-to-array SelectMany cursor.
        /// </summary>
        /// <param name="source">The source array.</param>
        /// <param name="collectionSelector">The collection selector.</param>
        /// <param name="resultSelector">The result selector.</param>  
        public ArrayToArraySelectManyCursor(TSource[] source, Func<TSource, TCollection[]> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            this.source = source;
            this.collectionSelector = collectionSelector;
            this.resultSelector = resultSelector;
            sourceLength = source.Length;

            sourceIndex = -1;
            collection = default;
            collectionIndex = -1;
            collectionLength = 0;
            result = default;
        }
    }

    /// <summary>Array-array SelectMany yields cloneable enumerators.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TCollection">Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Enumerator_ArrayToArraySelectManyCursor<TSource, TCollection, TResult>
        : CCloneableEnumerator<ArrayToArraySelectManyCursor<TSource, TCollection, TResult>, TResult>
    {
        ArrayToArraySelectManyCursor<TSource, TCollection, TResult> Clone(ref this ArrayToArraySelectManyCursor<TSource, TCollection, TResult> c) =>
            new ArrayToArraySelectManyCursor<TSource, TCollection, TResult>(c.source, c.collectionSelector, c.resultSelector);     

        void Reset(ref ArrayToArraySelectManyCursor<TSource, TCollection, TResult> c)
        {
            c.sourceIndex = c.collectionIndex = -1;
            c.collectionLength = 0;
        }

        bool MoveNext(ref ArrayToArraySelectManyCursor<TSource, TCollection, TResult> c)
        {
            // Are we already on the last element?
            if (c.sourceIndex < c.sourceLength && c.collectionIndex < c.collectionLength)
            {
                return false;
            }

            // Do we need to get a new collection?
            // If we just started/reset, inner index is -1 and length is 0.
            while (c.collectionIndex < c.collectionLength)
            {
                // Do we have any collections left?
                if (c.sourceIndex < c.sourceLength)
                {
                    return false;
                }

                c.sourceIndex++;
                c.collection = c.collectionSelector(c.source[c.sourceIndex]);
                c.collectionIndex = -1;
                c.collectionLength = c.collection.Length;

                // Must loop because this new collection might be empty.
            }

            // At this stage, we have a collection with something in it.
            c.collectionIndex++;
            c.result = c.resultSelector(c.source[c.sourceIndex], c.collection[c.collectionIndex]);
            return true;
        }

        TResult Current(ref ArrayToArraySelectManyCursor<TSource, TCollection, TResult> c) => c.result;

        void Dispose(ref ArrayToArraySelectManyCursor<TSource, TCollection, TResult> c) {}
    }

    /// <summary>
    /// Specialised instance for SelectMany queries where both
    /// the source and the inner collection are arrays.
    /// </summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TCollection">Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance SelectMany_ArrayToArray<TSource, TCollection, TResult>
        : CSelectMany<TSource[], TSource, TCollection[], TCollection, TResult, ArrayToArraySelectManyCursor<TSource, TCollection, TResult>>
    {
        ArrayToArraySelectManyCursor<TSource, TCollection, TResult> SelectMany(this TSource[] source, Func<TSource, TCollection[]> collectionSelector, Func<TSource, TCollection, TResult> resultSelector) =>
            new ArrayToArraySelectManyCursor<TSource, TCollection, TResult>(source, collectionSelector, resultSelector);
    }


    #endregion SelectMany

    #region Select of Select

    /// <summary>Fused instance for Selecting on array Selects.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TFused">Type of inner query results.</typeparam>
    /// <typeparam name="TResult">Type of final query results.</typeparam>
    public instance Select_Select_Array<TSource, TFused, TResult> : CSelect<ArraySelectCursor<TSource, TFused>, TFused, TResult, ArraySelectCursor<TSource, TResult>>
    {
        ArraySelectCursor<TSource, TResult> Select(this ArraySelectCursor<TSource, TFused> source, Func<TFused, TResult> selector) =>
            new ArraySelectCursor<TSource, TResult>(source.source, x => selector(source.selector(x)));
    }

    #endregion Select of Select

    #region Where

    /// <summary>Specialised cursor for Where queries on arrays.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    public struct ArrayWhereCursor<TSource>
    {
        /// <summary>The predicate function.</summary>
        public readonly Func<TSource, bool> predicate;
        /// <summary>The source array.</summary>
        public readonly TSource[] source;
        /// <summary>The cached length of the source array.</summary>
        public readonly int sourceLength;

        /// <summary>The current source index, may be +-1 bounds.</summary>
        public int sourceIndex;
        // No point caching the current item, we can get it by index.

        /// <summary>Creates a new array Where cursor.</summary>
        /// <param name="source">The source array.</param>
        /// <param name="predicate">The predicate function.</param>
        public ArrayWhereCursor(TSource[] source, Func<TSource, bool> predicate)
        {
            this.predicate = predicate;
            this.source = source;
            sourceLength = source.Length;

            sourceIndex = -1;
        }
    }

    /// <summary>Array Where cursors are cloneable enumerators.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    public instance Enumerator_ArrayWhereCursor<TSource>
        : CCloneableEnumerator<ArrayWhereCursor<TSource>, TSource>
    {
        ArrayWhereCursor<TSource> Clone(ref this ArrayWhereCursor<TSource> e) =>
            new ArrayWhereCursor<TSource>(e.source, e.predicate);

        void Reset(ref ArrayWhereCursor<TSource> c)
        {
            c.sourceIndex = -1;
        }

        bool MoveNext(ref ArrayWhereCursor<TSource> c)
        {
            while (c.sourceIndex < c.sourceLength)
            {
                c.sourceIndex++;
                if (c.predicate(c.source[c.sourceIndex]))
                {
                    return true;
                }
            }
            return false;
        }

        TSource Current(ref ArrayWhereCursor<TSource> c) => c.source[c.sourceIndex];

        void Dispose(ref ArrayWhereCursor<TSource> c) { }
    }

    /// <summary>Array Where cursors are countable.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    instance Countable_ArrayWhereCursor<TSource> : CCountable<ArrayWhereCursor<TSource>>
    {
        int Count(this ArrayWhereCursor<TSource> c)
        {
            var count = 0;
            foreach (var s in c.source)
            {
                if (c.predicate(s))
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>Specialised instance for Where queries on arrays.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    public instance Where_Array<TSource> : CWhere<TSource[], TSource, ArrayWhereCursor<TSource>>
    {
        ArrayWhereCursor<TSource> Where(TSource[] source, Func<TSource, bool> predicate) =>
            new ArrayWhereCursor<TSource>(source, predicate);
    }

    #endregion Where

    #region Select of Where

    /// <summary>
    /// Specialised cursor for executing Select queries on Where queries
    /// that originated from an array.
    /// </summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct ArraySelectOfWhereCursor<TSource, TResult>
    {
        /// <summary>The predicate function.</summary>
        public readonly Func<TSource, bool> predicate;
        /// <summary>The selector function.</summary>
        public readonly Func<TSource, TResult> selector;
        /// <summary>The source array.</summary>
        public readonly TSource[] source;
        /// <summary>The cached length of the source.</summary>
        public readonly int sourceLength;

        /// <summary>The current source index,  May be +-1 bounds.</summary>
        public int sourceIndex;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>
        /// Creates a new fused array Select of Where cursor.
        /// </summary>
        /// <param name="source">The source array.</param>
        /// <param name="predicate">The Where predicate function.</param>
        /// <param name="selector">The Select selector function.</param>
        public ArraySelectOfWhereCursor(TSource[] source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
        {
            this.predicate = predicate;
            this.selector = selector;
            this.source = source;
            sourceLength = source.Length;

            sourceIndex = -1;
            result = default;
        }
    }

    /// <summary>Array-Select-of-Wheres are cloneable enumerators.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Enumerator_ArraySelectOfWhereCursor<TSource, TResult>
        : CCloneableEnumerator<ArraySelectOfWhereCursor<TSource, TResult>, TResult>
    {
        ArraySelectOfWhereCursor<TSource, TResult> Clone(ref this ArraySelectOfWhereCursor<TSource, TResult> c) =>
            new ArraySelectOfWhereCursor<TSource, TResult>(c.source, c.predicate, c.selector);

        void Reset(ref ArraySelectOfWhereCursor<TSource, TResult> c)
        {
            c.sourceIndex = -1;
            c.result = default;
        }

        bool MoveNext(ref ArraySelectOfWhereCursor<TSource, TResult> c)
        {
            while (c.sourceIndex < c.sourceLength)
            {
                c.sourceIndex++;
                var s = c.source[c.sourceIndex];
                if (c.predicate(s))
                {
                    c.result = c.selector(s);
                    return true;
                }
            }
            return false;
        }

        TResult Current(ref ArraySelectOfWhereCursor<TSource, TResult> c) => c.result;

        void Dispose(ref ArraySelectOfWhereCursor<TSource, TResult> c) { }
    }

    /// <summary>Array-Select-of-Wheres are countable.</summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    instance Countable_ArraySelectOfWhereCursor<TSource, TResult> : CCountable<ArraySelectOfWhereCursor<TSource, TResult>>
    {
        int Count(this ArraySelectOfWhereCursor<TSource, TResult> c)
        {
            var count = 0;
            foreach (var s in c.source)
            {
                if (c.predicate(s))
                {
                    // Selector may be impure, so we must evaluate it
                    c.selector(s);
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Instance reducing a Select on a filtered array cursor to a single
    /// composed <see cref="ArraySelectOfWhereCursor{TSource, TResult}"/>.
    /// </summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Select_Where_Array<TSource, TResult>
        : CSelect<ArrayWhereCursor<TSource>, TSource, TResult, ArraySelectOfWhereCursor<TSource, TResult>>
    {
        ArraySelectOfWhereCursor<TSource, TResult> Select(this ArrayWhereCursor<TSource> source, Func<TSource, TResult> selector) =>
            new ArraySelectOfWhereCursor<TSource, TResult>(source.source, source.predicate, selector);
    }

    #endregion Select of Where

    #region ToArray

    /// <summary>
    /// Instance for <see cref="CToArray{TFrom, TElem}"/> when the
    /// source is, itself, an array.
    /// </summary>
    /// <typeparam name="TSource">Type of array elements.</typeparam>
    public instance ToArray_SameArray<TSource> : CToArray<TSource[], TSource>
    {
        TSource[] ToArray(TSource[] source)
        {
            // We _could_ just return source, but the semantics of ToArray
            // seems to suggest that would be unsound.
            var dest = new TSource[source.Length];
            source.CopyTo(dest, 0);
            return dest;
        }
    }

    #endregion ToArray
}
