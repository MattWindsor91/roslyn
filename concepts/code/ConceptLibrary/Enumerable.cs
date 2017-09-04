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
    /// Instances for common enumerables.
    /// </summary>
    public static class Instances
    {
        [Overlappable]
        public instance Enumerable_IEnumerable<TColl, TElem> : CEnumerable<TColl, IEnumerator<TElem>, TElem>
            where TColl : IEnumerable<TElem>
        {
            IEnumerator<TElem> GetEnumerator(TColl coll) => coll.GetEnumerator();
            void Reset(ref IEnumerator<TElem> e) => e.Reset();
            bool MoveNext(ref IEnumerator<TElem> e) => e.MoveNext();
            TElem Current(ref IEnumerator<TElem> e) => e.Current;
            void Dispose(ref IEnumerator<TElem> e) => e.Dispose();
        }

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
