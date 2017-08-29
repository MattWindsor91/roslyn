using System;
using System.Concepts.Enumerable;

namespace TinyLinq.SpecialisedInstances
{
    // Specialised instances for executing queries on arrays.
    //
    // These reduce various query patterns into a single struct.

    #region Where

    /// <summary>
    /// Specialised enumerator for executing Where queries on an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public struct ArrayWhere<TElem>
    {
        public TElem[] source;
        public Func<TElem, bool> filter;
        public int lo;
        public int hi;
    }

    /// <summary>
    /// Enumerator instance for filtered arrays.
    /// </summary>
    public instance Enumerator_ArrayWhere<TElem> : CEnumerator<TElem, ArrayWhere<TElem>>
    {
        void Reset(ref ArrayWhere<TElem> enumerator)
        {
            enumerator.lo = -1;
        }

        bool MoveNext(ref ArrayWhere<TElem> enumerator)
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

        TElem Current(ref ArrayWhere<TElem> enumerator)
        {
            if (enumerator.lo == -1)
            {
                return default;
            }
            return enumerator.source[enumerator.lo];
        }

        void Dispose(ref ArrayWhere<TElem> enumerator) { }
    }

    /// <summary>
    /// Specialised instance for executing Where queries on an array.
    /// </summary>
    public instance Where_Array<TElem> : CWhere<TElem, TElem[], ArrayWhere<TElem>>
    {
        ArrayWhere<TElem> Where(TElem[] src, Func<TElem, bool> f) =>
            new ArrayWhere<TElem> { source = src, filter = f, lo = -1, hi = src.Length };
    }

    #endregion Where

    #region Select of Where

    public struct ArraySelectOfWhere<TElem, TProj>
    {
        public TElem[] source;
        public Func<TElem, bool> filter;
        public Func<TElem, TProj> projection;
        public int lo;
        public int hi;
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for selections of filtered arrays.
    /// </summary>
    public instance Enumerator_ArraySelectOfWhere<TElem, TProj> : CEnumerator<TProj, ArraySelectOfWhere<TElem, TProj>>
    {
        void Reset(ref ArraySelectOfWhere<TElem, TProj> sw)
        {
            sw.lo = -1;
            sw.current = default;
        }

        bool MoveNext(ref ArraySelectOfWhere<TElem, TProj> sw)
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

        TProj Current(ref ArraySelectOfWhere<TElem, TProj> sw) => sw.current;

        void Dispose(ref ArraySelectOfWhere<TElem, TProj> enumerator) { }
    }

    /// <summary>
    /// Instance reducing a Select on a filtered array cursor to a single
    /// composed <see cref="SelectedFilteredArrayCursor{TElem, TProj}"/>.
    /// </summary>
    public instance Select_Where_Array<TElem, TProj> : CSelect<TElem, TProj, ArrayWhere<TElem>, ArraySelectOfWhere<TElem, TProj>>
    {
        ArraySelectOfWhere<TElem, TProj> Select(ArrayWhere<TElem> t, Func<TElem, TProj> projection) =>
            new ArraySelectOfWhere<TElem, TProj>
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
