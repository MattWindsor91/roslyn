using System.Collections.Generic;
using System.Concepts;

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
    public concept CEnumerable<TColl, [AssociatedType] TState, [AssociatedType] TElem> : CEnumerator<TState, TElem>
    {
        TState GetEnumerator(TColl container);
    }

    /// <summary>
    /// A generic range.
    /// </summary>
    /// <typeparam name="TNum">
    /// Type of numbers in the range.
    /// </typeparam>
    public struct Range<TNum>
    {
        /// <summary>
        /// The start of the range.
        /// </summary>
        public TNum start;
        /// <summary>
        /// The number of items in the range.
        /// </summary>
        public int count;
    }

    /// <summary>
    /// Instances for common enumerables.
    /// </summary>
    public static class Instances
    {
        // TODO: this should be TColl where TColl : IEnumerable<TElem>
        //       but the current inferrer can't relate TColl and TElem
        //       properly using the constraint.
        [Overlappable]
        public instance Enumerable_IEnumerable<TElem> : CEnumerable<IEnumerable<TElem>, IEnumerator<TElem>, TElem>

        {
            IEnumerator<TElem> GetEnumerator(IEnumerable<TElem> coll) => coll.GetEnumerator();
            void Reset(ref IEnumerator<TElem> e) => e.Reset();
            bool MoveNext(ref IEnumerator<TElem> e) => e.MoveNext();
            TElem Current(ref IEnumerator<TElem> e) => e.Current;
            void Dispose(ref IEnumerator<TElem> e) => e.Dispose();
        }

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
        public instance Enumerable_Range<TNum, implicit N, implicit E> : CEnumerable<Range<TNum>, RangeCursor<TNum>, TNum>, CCopyEnumerator<RangeCursor<TNum>, TNum>
            where N : Prelude.Num<TNum>
            where E : Prelude.Eq<TNum>
        {
            // TODO: catch inverted ranges and overflows
            // TODO: better optimisation if range is empty

            RangeCursor<TNum> GetEnumerator(Range<TNum> range) => new RangeCursor<TNum> { range = range, end = Add(range.start, FromInteger(range.count)), reset = true, finished = false };
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
                    e.current = Add(e.current, FromInteger(1));
                }

                e.finished = Equals(e.end, e.current);
                return !e.finished;
            }
                
            TNum Current(ref RangeCursor<TNum> e) => e.current;
            void Dispose(ref RangeCursor<TNum> e) { }

            // RangeCursor is a value type.
            RangeCursor<TNum> Copy(this RangeCursor<TNum> e) => e;
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
        /// <see cref="CEnumerable{TColl, TState, TElem}"/> instance for arrays,
        /// using array cursors.
        /// Also serves as a <see cref="CEnumerator{TState, TElem}"/> instance for
        /// array cursors, because it has the same types.
        /// </summary>
        public instance Enumerable_Array<TElem> : CEnumerable<TElem[], ArrayCursor<TElem>, TElem>
        {
            ArrayCursor<TElem> GetEnumerator(TElem[] array) => new ArrayCursor<TElem> { source = array, lo = -1, hi = array.Length };

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

        #endregion Arrays
        #region Generic collections

        /// <summary>
        /// <see cref="CEnumerable{TColl, TState, TElem}"/> instance for lists,
        /// using list enumerators.
        /// </summary>
        public instance Enumerable_List<TElem> : CEnumerable<List<TElem>, List<TElem>.Enumerator, TElem>
        {
            List<TElem>.Enumerator GetEnumerator(List<TElem> list) => list.GetEnumerator();

            void Reset(ref List<TElem>.Enumerator enumerator) => ((IEnumerator<TElem>)enumerator).Reset();
            bool MoveNext(ref List<TElem>.Enumerator enumerator) => enumerator.MoveNext();
            TElem Current(ref List<TElem>.Enumerator enumerator) => enumerator.Current;
            void Dispose(ref List<TElem>.Enumerator enumerator) => enumerator.Dispose();
        }

        #endregion Generic collections
    }
}
