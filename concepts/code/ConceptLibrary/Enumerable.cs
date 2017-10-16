using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Indexable;
using System.Concepts.Prelude;

namespace System.Concepts.Enumerable
{
    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="TState">
    ///     The state held by the enumerator.
    /// </typeparam>
    /// <typeparam name="TElem">
    ///     The element returned by the enumerator.
    /// </typeparam>
    public concept CEnumerator<TState, [AssociatedType] TElem>
    {
        void Reset(ref TState enumerator);
        bool MoveNext(ref TState enumerator);
        TElem Current(ref TState enumerator);
        void Dispose(ref TState enumerator);
    }

    /// <summary>
    /// Concept for enumerators that can be shallow-copied.
    /// </summary>
    public concept CCopyEnumerator<TState, [AssociatedType] TElem> : CEnumerator<TState, TElem>
    {
        /// <summary>
        /// Shallow-copies the enumerator.
        /// </summary>
        /// <remarks>
        /// As this method does not take its enumerator by reference,
        /// value-type enumerators can just in-place modify and
        /// return <paramref name="enumerator"/>.
        /// </remarks>
        /// <param name="enumerator">
        /// The enumerator to shallow-copy.
        /// </param>
        /// <returns>
        /// A copy of the enumerator.
        /// The copy can hold the same reference to the original data
        /// as the previous copy, but must have separate storage for its
        /// enumeration state.
        /// </returns>
        TState Copy(this TState enumerator);

        /// <summary>
        /// Shallow-copies the enumerator, also resetting it.
        /// </summary>
        /// <param name="enumerator">
        /// The enumerator to shallow-copy.
        /// </param>
        /// <returns>
        /// A reset copy of the enumerator.
        /// </returns>
        TState CopyAndReset(this TState enumerator)
        {
            // TODO: enumerator.Copy() should work
            // TODO: var en = this.Copy(enumerator) gives an occurs check violation
            var en = Copy(enumerator);
            Reset(ref en);
            return en;
        }
    }

    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="TColl">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="TState">
    ///     The state held by the enumerator.
    /// </typeparam>
    /// <typeparam name="TElem">
    ///     The element returned by the enumerator.
    /// </typeparam>
    public concept CEnumerable<TColl, [AssociatedType] TState>
    {
        TState GetEnumerator(this TColl container);
    }

    /// <summary>
    /// Instances for common enumerables.
    /// </summary>
    public static class Instances
    {
        [Overlappable]
        public instance Enumerator_IEnumerator<TElem> : CEnumerator<IEnumerator<TElem>, TElem>
        {
            void Reset(ref IEnumerator<TElem> e) => e.Reset();
            bool MoveNext(ref IEnumerator<TElem> e) => e.MoveNext();
            TElem Current(ref IEnumerator<TElem> e) => e.Current;
            void Dispose(ref IEnumerator<TElem> e) => e.Dispose();
        }

        // TODO: this should be TColl where TColl : IEnumerable<TElem>
        //       but the current inferrer can't relate TColl and TElem
        //       properly using the constraint.
        [Overlappable]
        public instance Enumerable_IEnumerable<TElem> : CEnumerable<IEnumerable<TElem>, IEnumerator<TElem>>
        {
            IEnumerator<TElem> GetEnumerator(this IEnumerable<TElem> coll) => coll.GetEnumerator();
        }

        /// <summary>
        /// Any enumerator that can be shallow copied is a trivial enumerable,
        /// where getting the enumerator is equal to copying.
        /// </summary>
        [Overlappable]
        public instance Enumerable_CopyEnumerator<TEnum, TElem, implicit C> : CEnumerable<TEnum, TEnum>
            where C : CCopyEnumerator<TEnum, TElem>
        {
            TEnum GetEnumerator(this TEnum e) => e.Copy();
        }

        #region Enumerables from index and bound

        /// <summary>
        /// Cursor for index-and-bound enumeration.
        /// </summary>
        /// <typeparam name="TColl">The type being indexed.</typeparam>
        /// <typeparam name="TIdx">The type of indexes.</typeparam>
        /// <typeparam name="TElem">The type of elements.</typeparam>
        public struct IndexBoundCursor<TColl, TIdx, TElem>
        {
            public TColl container;
            public TIdx pos;
            public TIdx len;
            public TElem current;
        }

        public instance Enumerator_IndexBoundCursor<TColl, TIdx, TElem, implicit I, implicit N, implicit E> : CEnumerator<IndexBoundCursor<TColl, TIdx, TElem>, TElem>
            where I : CIndexable<TColl, TIdx, TElem>
            where N : Num<TIdx>
            where E : Eq<TIdx>
        {
            void Reset(ref IndexBoundCursor<TColl, TIdx, TElem> e)
            {
                e.pos = N.FromInteger(-1);
                e.current = default;
            }
            bool MoveNext(ref IndexBoundCursor<TColl, TIdx, TElem> e)
            {
                if (e.pos == e.len)
                {
                    return false;
                }

                e.pos += N.FromInteger(1);
                if (e.pos == e.len)
                {
                    return false;
                }
                e.current = e.container.At(e.pos);
                return true;
            }
            TElem Current(ref IndexBoundCursor<TColl, TIdx, TElem> e) => e.current;
            void Dispose(ref IndexBoundCursor<TColl, TIdx, TElem> e) { }
        }

        [Overlappable]
        public instance Enumerable_IndexBound<TColl, [AssociatedType]TIdx, [AssociatedType]TElem, implicit I, implicit N, implicit E, implicit L> : CEnumerable<TColl, IndexBoundCursor<TColl, TIdx, TElem>>
            where I : CIndexable<TColl, TIdx, TElem>
            where N : Num<TIdx>
            where E : Eq<TIdx>
            where L : CStaticCountable<TColl>
        {
            IndexBoundCursor<TColl, TIdx, TElem> GetEnumerator(TColl container) => new IndexBoundCursor<TColl, TIdx, TElem> { container = container, len = N.FromInteger(container.Count()), pos = N.FromInteger(-1) };
        }


        #endregion Enumerables from length and bound

        #region Ranges

        public struct RangeCursor<TNum>
        {
            /// <summary>
            /// The range over which we are iterating.
            /// </summary>
            public Range<TNum> range;
            /// <summary>
            /// The cached end of the range.
            /// </summary>
            public TNum end;
            /// <summary>
            /// The current item in the range.
            /// </summary>
            public TNum current;
            /// <summary>
            /// Whether we are one below the first item in the range.
            /// </summary>
            public bool reset;
            /// <summary>
            /// Whether we are one after the last item in the range.
            /// </summary>
            public bool finished;
        }

        /// <summary>
        /// Various enumerator instances for ranges.
        /// </summary>
        public instance CopyEnumerator_Range<TNum, implicit N, implicit E> : CCopyEnumerator<RangeCursor<TNum>, TNum>
            where N : Num<TNum>
            where E : Eq<TNum>
        {
            // TODO: catch inverted ranges and overflows
            // TODO: better optimisation if range is empty

            void Reset(ref RangeCursor<TNum> e)
            {
                e.reset = true;
                e.finished = false;
            }
            bool MoveNext(ref RangeCursor<TNum> e)
            {
                if (e.finished)
                {
                    return false;
                }

                if (e.reset)
                {
                    e.current = e.range.start;
                    e.reset = false;
                }
                else
                {
                    e.current += FromInteger(1);
                }

                e.finished = Equals(e.end, e.current);
                return !e.finished;
            }
                
            TNum Current(ref RangeCursor<TNum> e) => e.current;
            void Dispose(ref RangeCursor<TNum> e) { }

            // RangeCursor is a value type.
            RangeCursor<TNum> Copy(this RangeCursor<TNum> e) => e;
        }

        /// <summary>
        /// Various enumerator instances for ranges.
        /// </summary>
        public instance Enumerable_Range<TNum, implicit N> : CEnumerable<Range<TNum>, RangeCursor<TNum>>
            where N : Num<TNum>
        {
            RangeCursor<TNum> GetEnumerator(this Range<TNum> range) =>
                new RangeCursor<TNum> { range = range, end = range.start + FromInteger(range.count), reset = true, finished = false };
        }

        #endregion Ranges

        #region Arrays

        /// <summary>
        /// A structure representing the enumerator state for an array.
        /// </summary>
        /// <typeparam name="TElem">
        /// Type of array elements.
        /// </typeparam>
        public struct ArrayCursor<TElem>
        {
            public TElem[] source;
            public int lo;
            public int hi;
        }

        /// <summary>
        /// <see cref="CEnumerator{TColl, TState}"/> instance for array
        /// cursors.
        /// </summary>
        public instance Enumerator_ArrayCursor<TElem> : CCopyEnumerator<ArrayCursor<TElem>, TElem>
        {
            // ArrayCursor is a struct, so it inherently gets copied
            ArrayCursor<TElem> Copy(this ArrayCursor<TElem> c) => c;

            void Reset(ref ArrayCursor<TElem> enumerator)
            {
                enumerator.lo = -1;
            }

            bool MoveNext(ref ArrayCursor<TElem> enumerator)
            {
                // hi always points to one index beyond the end of the array slice
                if (enumerator.hi <= enumerator.lo)
                {
                    return false;
                }

                enumerator.lo++;
                return (enumerator.lo < enumerator.hi);
            }

            TElem Current(ref ArrayCursor<TElem> enumerator)
            {
                if (enumerator.lo == -1)
                {
                    return default;
                }
                return enumerator.source[enumerator.lo];
            }

            void Dispose(ref ArrayCursor<TElem> enumerator) { }
        }

        /// <summary>
        /// <see cref="CEnumerable{TColl, TState}"/> instance for arrays,
        /// using array cursors.
        /// </summary>
        public instance Enumerable_Array<TElem> : CEnumerable<TElem[], ArrayCursor<TElem>>
        {
            ArrayCursor<TElem> GetEnumerator(this TElem[] array) => new ArrayCursor<TElem> { source = array, lo = -1, hi = array.Length };
        }

        #endregion Arrays
        #region Generic collections

        /// <summary>
        /// <see cref="CEnumerator{TState, TElem}"/> instance for list
        /// enumerators.
        /// </summary>
        public instance Enumerator_List<TElem> : CCopyEnumerator<List<TElem>.Enumerator, TElem>
        {
            // Enumerator is a struct, so it implicitly gets shallow-copied.
            List<TElem>.Enumerator Copy(this List<TElem>.Enumerator e) => e;

            void Reset(ref List<TElem>.Enumerator enumerator) => ((IEnumerator<TElem>)enumerator).Reset();
            bool MoveNext(ref List<TElem>.Enumerator enumerator) => enumerator.MoveNext();
            TElem Current(ref List<TElem>.Enumerator enumerator) => enumerator.Current;
            void Dispose(ref List<TElem>.Enumerator enumerator) => enumerator.Dispose();
        }

        /// <summary>
        /// <see cref="CEnumerable{TColl, TState}"/> instance for lists,
        /// using list enumerators.
        /// </summary>
        public instance Enumerable_List<TElem> : CEnumerable<List<TElem>, List<TElem>.Enumerator>
        {
            List<TElem>.Enumerator GetEnumerator(List<TElem> list) => list.GetEnumerator();
        }

        #endregion Generic collections
    }
}
