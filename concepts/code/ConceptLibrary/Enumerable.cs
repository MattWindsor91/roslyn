using System.Collections.Generic;
using System.Concepts;

namespace System.Concepts.Enumerable
{
    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    public concept CEnumerator<E, [AssociatedType] S>
    {
        void Reset(ref S enumerator);
        bool MoveNext(ref S enumerator);
        E Current(ref S enumerator);
        void Dispose(ref S enumerator);
    }

    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    public concept CEnumerable<C, [AssociatedType] E, [AssociatedType] S> : CEnumerator<E, S>
    {
        S GetEnumerator(C container);
    }

    /// <summary>
    /// Instances for common enumerables.
    /// </summary>
    public static class Instances
    {
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
        /// <see cref="CEnumerator{E, S}"/> instance for array cursors.
        /// </summary>
        public instance Enumerator_ArrayCursor<TElem> : CEnumerator<TElem, ArrayCursor<TElem>>
        {
            void Reset(ref ArrayCursor<TElem> enumerator)
            {
                enumerator.lo = -1;
            }

            bool MoveNext(ref ArrayCursor<TElem> enumerator)
            {
                // hi always points to one index beyond the end of the array slice
                if (enumerator.hi <= enumerator.lo + 1)
                {
                    return false;
                }
                enumerator.lo++;
                return true;
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
        /// <see cref="CEnumerable{C, E, S}"/> instance for arrays,
        /// using array cursors.
        /// </summary>
        public instance Enumerable_Array<TElem> : CEnumerable<TElem[], TElem, ArrayCursor<TElem>>
        {
            ArrayCursor<TElem> GetEnumerator(TElem[] array) => new ArrayCursor<TElem> { source = array, lo = -1, hi = array.Length };
            void Reset(ref ArrayCursor<TElem> enumerator) => Enumerator_ArrayCursor<TElem>.Reset(ref enumerator);
            bool MoveNext(ref ArrayCursor<TElem> enumerator) => Enumerator_ArrayCursor<TElem>.MoveNext(ref enumerator);
            TElem Current(ref ArrayCursor<TElem> enumerator) => Enumerator_ArrayCursor<TElem>.Current(ref enumerator);
            void Dispose(ref ArrayCursor<TElem> enumerator) => Enumerator_ArrayCursor<TElem>.Dispose(ref enumerator);
        }

        #endregion Arrays
        #region Generic collections

        /// <summary>
        /// <see cref="CEnumerator{E, S}"/> instance for list enumerators.
        /// </summary>
        public instance Enumerator_ListEnumerator<TElem> : CEnumerator<TElem, List<TElem>.Enumerator>
        {
            void Reset(ref List<TElem>.Enumerator enumerator) => ((IEnumerator<TElem>) enumerator).Reset();
            bool MoveNext(ref List<TElem>.Enumerator enumerator) => enumerator.MoveNext();
            TElem Current(ref List<TElem>.Enumerator enumerator) => enumerator.Current;
            void Dispose(ref List<TElem>.Enumerator enumerator) => enumerator.Dispose();
        }

        /// <summary>
        /// <see cref="CEnumerable{C, E, S}"/> instance for lists,
        /// using list enumerators.
        /// </summary>
        public instance Enumerable_List<TElem> : CEnumerable<List<TElem>, TElem, List<TElem>.Enumerator>
        {
            List<TElem>.Enumerator GetEnumerator(List<TElem> list) => list.GetEnumerator();
            void Reset(ref List<TElem>.Enumerator enumerator) => Enumerator_ListEnumerator<TElem>.Reset(ref enumerator);
            bool MoveNext(ref List<TElem>.Enumerator enumerator) => Enumerator_ListEnumerator<TElem>.MoveNext(ref enumerator);
            TElem Current(ref List<TElem>.Enumerator enumerator) => Enumerator_ListEnumerator<TElem>.Current(ref enumerator);
            void Dispose(ref List<TElem>.Enumerator enumerator) => Enumerator_ListEnumerator<TElem>.Dispose(ref enumerator);
        }

        #endregion Generic collections
    }
}
