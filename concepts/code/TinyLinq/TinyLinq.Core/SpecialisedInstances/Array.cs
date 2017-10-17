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

    /// <summary>
    /// Specialised enumerator for executing Select queries on arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public struct ArraySelectCursor<TElem, TProj>
    {
        /// <summary>
        /// The source array.
        /// </summary>
        public TElem[] source;
        /// <summary>
        /// The projection function from the Select query.
        /// </summary>
        public Func<TElem, TProj> projection;
        /// <summary>
        /// The current array index.
        /// </summary>
        public int lo;
        /// <summary>
        /// The cached length of the array.
        /// </summary>
        public int hi;
        /// <summary>
        /// The cached current item.
        /// </summary>
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for selections of arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public instance Enumerator_ArraySelectCursor<TElem, TProj> : CEnumerator<ArraySelectCursor<TElem, TProj>, TProj>
    {
        void Reset(ref ArraySelectCursor<TElem, TProj> sw)
        {
            sw.lo = -1;
        }

        bool MoveNext(ref ArraySelectCursor<TElem, TProj> sw)
        {
            if (sw.hi <= sw.lo)
            {
                return false;
            }

            sw.lo++;

            if (sw.hi <= sw.lo)
            {
                return false;
            }

            sw.current = sw.projection(sw.source[sw.lo]);
            return true;
        }

        TProj Current(ref ArraySelectCursor<TElem, TProj> sw) => sw.current;

        void Dispose(ref ArraySelectCursor<TElem, TProj> enumerator) { }
    }

    /// <summary>
    /// Instance reducing a Select on an array cursor to a single
    /// composed <see cref="ArraySelectCursor{TElem, TProj}"/>.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public instance Select_ArrayCursor<TElem, TProj> : CSelect<TElem, TProj, Instances.ArrayCursor<TElem>, ArraySelectCursor<TElem, TProj>>
    {
        ArraySelectCursor<TElem, TProj> Select(this Instances.ArrayCursor<TElem> t, Func<TElem, TProj> projection) =>
            new ArraySelectCursor<TElem, TProj>
            {
                source = t.source,
                projection = projection,
                lo = -1,
                hi = t.source.Length
            };
    }

    /// <summary>
    /// Instance reducing a Select on an array to a single
    /// composed <see cref="ArraySelectCursor{TElem, TProj}"/>.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public instance Select_Array<TElem, TProj> : CSelect<TElem, TProj, TElem[], ArraySelectCursor<TElem, TProj>>
    {
        ArraySelectCursor<TElem, TProj> Select(this TElem[] t, Func<TElem, TProj> projection) =>
            new ArraySelectCursor<TElem, TProj>
            {
                source = t,
                projection = projection,
                lo = -1,
                hi = t.Length
            };
    }

    #endregion Select

    #region SelectMany

    public struct ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>
    {
        /// <summary>
        /// The source array.
        /// </summary>
        public TElem[] source;
        /// <summary>
        /// The current index into the source array.
        /// May be +/-1 past array bounds.
        /// </summary>
        public int sourceIndex;
        /// <summary>
        /// The cached length of the source array.
        /// </summary>
        public int sourceLength;

        /// <summary>
        /// The current inner array.
        /// </summary>
        public TInnerElem[] inner;
        /// <summary>
        /// The current index into the inner array.
        /// May be +/-1 past array bounds.
        /// </summary>
        public int innerIndex;
        /// <summary>
        /// The cached length of the inner array.
        /// </summary>
        public int innerLength;

        /// <summary>
        /// The current element.
        /// </summary>
        public TProj current;

        /// <summary>
        /// The outer projection.
        /// </summary>
        public Func<TElem, TInnerElem[]> outerProjection;
        /// <summary>
        /// The inner projection.
        /// </summary>
        public Func<TElem, TInnerElem, TProj> innerProjection;
    }

    public instance Enumerator_ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>
        : CEnumerator<ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>, TProj>
    {
        void Reset(ref ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj> sm)
        {
            sm.sourceIndex = sm.innerIndex = -1;
        }

        bool MoveNext(ref ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj> sm)
        {
            // Outer array has finished: we're done.
            if (sm.sourceLength <= sm.sourceIndex)
            {
                return false;
            }

            var mustCycleOuter = sm.sourceIndex < 0;

            // Keep going until we run out of outer elements.
            while (true)
            {
                if (mustCycleOuter)
                {
                    sm.sourceIndex++;
                    if (sm.sourceLength <= sm.sourceIndex)
                    {
                        // We've run out of outer elements, so we're done.
                        return false;
                    }
                    sm.inner = sm.outerProjection(sm.source[sm.sourceIndex]);
                    sm.innerIndex = -1;
                    sm.innerLength = sm.inner.Length;
                }
                mustCycleOuter = true;

                // Does the inner array have something left in it?
                sm.innerIndex++;
                if (sm.innerIndex < sm.innerLength)
                {
                    sm.current = sm.innerProjection(sm.source[sm.sourceIndex], sm.inner[sm.innerIndex]);
                    return true;
                }

                // It doesn't, so loop back and increment the outer array.
            }
        }

        TProj Current(ref ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj> sm) => sm.current;

        void Dispose(ref ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj> sm) {}
    }

    public instance SelectMany_ArrayCursorToArray<TElem, TInnerElem, TProj> : CSelectMany<Instances.ArrayCursor<TElem>, TElem, TInnerElem[], TInnerElem, TProj, ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>>
    {
        ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj> SelectMany(this Instances.ArrayCursor<TElem> t, Func<TElem, TInnerElem[]> outer, Func<TElem, TInnerElem, TProj> inner) =>
            new ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>
            {
                source = t.source,
                sourceIndex = -1,
                sourceLength = t.source.Length,

                outerProjection = outer,
                innerProjection = inner
            };
    }

    public instance SelectMany_ArrayToArray<TElem, TInnerElem, TProj> : CSelectMany<TElem[], TElem, TInnerElem[], TInnerElem, TProj, ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>>
    {
        ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj> SelectMany(this TElem[] t, Func<TElem, TInnerElem[]> outer, Func<TElem, TInnerElem, TProj> inner) =>
            new ArrayToArraySelectManyCursor<TElem, TInnerElem, TProj>
            {
                source = t,
                sourceIndex = -1,
                sourceLength = t.Length,

                outerProjection = outer,
                innerProjection = inner
            };
    }


    #endregion SelectMany

    #region Select of Select

    /// <summary>
    /// Instance reducing chained array Select queries to a single
    /// <see cref="ArraySelectCursor{TElem, TProj}"/> on a composed projection.
    /// </summary>
    public instance Select_Select_Array<TElem, TProj1, TProj2> : CSelect<TProj1, TProj2, ArraySelectCursor<TElem, TProj1>, ArraySelectCursor<TElem, TProj2>>
    {
        ArraySelectCursor<TElem, TProj2> Select(this ArraySelectCursor<TElem, TProj1> t, Func<TProj1, TProj2> projection) =>
            new ArraySelectCursor<TElem, TProj2>
            {
                source = t.source,
                projection = x => projection(t.projection(x)),
                lo = -1,
                hi = t.source.Length
            };
    }

    #endregion Select of Select

    #region Where

    /// <summary>
    /// Specialised enumerator for executing Where queries on an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public struct ArrayWhereCursor<TElem>
    {
        /// <summary>
        /// The source array.
        /// </summary>
        public TElem[] source;
        /// <summary>
        /// The filtering predicate.
        /// </summary>
        public Func<TElem, bool> filter;
        /// <summary>
        /// The current index of the enumerator.
        /// </summary>
        public int lo;
        /// <summary>
        /// The length of the array.
        /// </summary>
        public int hi;
    }

    /// <summary>
    /// Enumerator instance for filtered arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance Enumerator_ArrayWhereCursor<TElem> : CEnumerator<ArrayWhereCursor<TElem>, TElem>
    {
        void Reset(ref ArrayWhereCursor<TElem> enumerator)
        {
            enumerator.lo = -1;
        }

        bool MoveNext(ref ArrayWhereCursor<TElem> enumerator)
        {
            if (enumerator.hi <= enumerator.lo)
            {
                return false;
            }

            enumerator.lo++;
            while (enumerator.lo < enumerator.hi)
            {
                if (enumerator.filter(enumerator.source[enumerator.lo]))
                {
                    return true;
                }
                enumerator.lo++;
            }

            return false;
        }

        TElem Current(ref ArrayWhereCursor<TElem> enumerator)
        {
            if (enumerator.lo == -1)
            {
                return default;
            }
            return enumerator.source[enumerator.lo];
        }

        void Dispose(ref ArrayWhereCursor<TElem> enumerator) { }
    }

    /// <summary>
    /// Count instance for filtered arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    instance Countable_ArrayWhereCursor<TElem> : CCountable<ArrayWhereCursor<TElem>>
    {
        int Count(this ArrayWhereCursor<TElem> aw)
        {
            var count = 0;
            foreach (var s in aw.source)
            {
                if (aw.filter(s))
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Specialised instance for executing Where queries on an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance Where_Array<TElem> : CWhere<TElem[], TElem, ArrayWhereCursor<TElem>>
    {
        ArrayWhereCursor<TElem> Where(TElem[] src, Func<TElem, bool> f) =>
            new ArrayWhereCursor<TElem> { source = src, filter = f, lo = -1, hi = src.Length };
    }

    #endregion Where

    #region Select of Where

    /// <summary>
    /// Specialised enumerator for executing Select queries on Where queries
    /// that originated from an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public struct ArraySelectOfWhereCursor<TElem, TProj>
    {
        /// <summary>
        /// The source array.
        /// </summary>
        public TElem[] source;
        /// <summary>
        /// The filtering predicate from the Where query.
        /// </summary>
        public Func<TElem, bool> filter;
        /// <summary>
        /// The projection function from the Select query.
        /// </summary>
        public Func<TElem, TProj> projection;
        /// <summary>
        /// The current array index.
        /// </summary>
        public int lo;
        /// <summary>
        /// The cached length of the array.
        /// </summary>
        public int hi;
        /// <summary>
        /// The cached current item.
        /// </summary>
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for selections of filtered arrays.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public instance Enumerator_ArraySelectOfWhereCursor<TElem, TProj> : CEnumerator<ArraySelectOfWhereCursor<TElem, TProj>, TProj>
    {
        void Reset(ref ArraySelectOfWhereCursor<TElem, TProj> sw)
        {
            sw.lo = -1;
            sw.current = default;
        }

        bool MoveNext(ref ArraySelectOfWhereCursor<TElem, TProj> sw)
        {
            if (sw.hi <= sw.lo)
            {
                return false;
            }

            sw.lo++;
            while (sw.lo < sw.hi)
            {
                if (sw.filter(sw.source[sw.lo]))
                {
                    sw.current = sw.projection(sw.source[sw.lo]);
                    return true;
                }
                sw.lo++;
            }

            return false;
        }

        TProj Current(ref ArraySelectOfWhereCursor<TElem, TProj> sw) => sw.current;

        void Dispose(ref ArraySelectOfWhereCursor<TElem, TProj> enumerator) { }
    }

    /// <summary>
    /// Instance reducing a Select on a filtered array cursor to a single
    /// composed <see cref="ArraySelectOfWhereCursor{TElem, TProj}"/>.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements exiting the select.
    /// </typeparam>
    public instance Select_Where_Array<TElem, TProj> : CSelect<TElem, TProj, ArrayWhereCursor<TElem>, ArraySelectOfWhereCursor<TElem, TProj>>
    {
        ArraySelectOfWhereCursor<TElem, TProj> Select(this ArrayWhereCursor<TElem> t, Func<TElem, TProj> projection) =>
            new ArraySelectOfWhereCursor<TElem, TProj>
            {
                source = t.source,
                filter = t.filter,
                projection = projection,
                lo = -1,
                hi = t.hi,
                current = default
            };
    }

    #endregion Select of Where

    #region ToArray

    /// <summary>
    /// Instance for <see cref="CToArray{TFrom, TElem}"/> when the
    /// source is, itself, an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance ToArray_SameArray<TElem> : CToArray<TElem[], TElem>
    {
        TElem[] ToArray(TElem[] from) => from;
    }

    #endregion ToArray
}
