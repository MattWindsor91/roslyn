using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Indexable;
using System.Concepts.Prelude;

namespace System.Concepts.Enumerable
{
    /// <summary>
    ///     Concept for types that may be enumerated.
    /// </summary>
    /// <typeparam name="TState">
    ///     The state held by the enumerator.
    /// </typeparam>
    /// <typeparam name="TElem">
    ///     The element returned by the enumerator.
    /// </typeparam>
    public concept CEnumerator<TState, [AssociatedType] TElem>
    {
        bool MoveNext(ref this TState enumerator);
        TElem Current(ref this TState enumerator);
        void Dispose(ref this TState enumerator);
    }

    /// <summary>
    ///     Concept for enumerators that support resetting..
    /// </summary>
    /// <typeparam name="TState">
    ///     The state held by the enumerator.
    /// </typeparam>
    /// <typeparam name="TElem">
    ///     The element returned by the enumerator.
    /// </typeparam>
    public concept CResettableEnumerator<TState, [AssociatedType] TElem> : CEnumerator<TState, TElem>
    {
        void Reset(ref this TState enumerator);
    }

    /// <summary>
    /// Concept for enumerators that can be cloned.
    /// </summary>
    public concept CCloneableEnumerator<TState, [AssociatedType] TElem> : CResettableEnumerator<TState, TElem>
    {
        /// <summary>
        /// Clones the enumerator.
        /// </summary>
        /// <remarks>
        /// The new enumerator should be pre-reset.
        /// </remarks>
        /// <param name="enumerator">
        /// The enumerator to shallow-copy.
        /// </param>
        /// <returns>
        /// A reset copy of the enumerator.
        /// </returns>
        TState Clone(ref this TState enumerator);
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

        TState RefGetEnumerator(ref this TColl container) => GetEnumerator(container);
    }

    /// <summary>
    /// Instances for common enumerables.
    /// </summary>
    public static class Instances
    {
        [Overlappable]
        public instance Enumerator_IEnumerator<TElem> : CEnumerator<IEnumerator<TElem>, TElem>
        {
            // Not all IEnumerators actually support resetting, so
            // we don't expose Reset by default.
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
        /// Any enumerator that can be cloned is a trivial enumerable,
        /// where getting the enumerator is equal to cloning.
        /// </summary>
        [Overlappable]
        public instance Enumerable_CopyEnumerator<TEnum, [AssociatedType]TElem, implicit C> : CEnumerable<TEnum, TEnum>
            where C : CCloneableEnumerator<TEnum, TElem>
        {
            TEnum GetEnumerator(this TEnum e) => e.Clone();
            TEnum RefGetEnumerator(ref this TEnum e) => e.Clone();
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

        public instance Enumerator_IndexBoundCursor<TColl, TIdx, TElem, implicit I, implicit N, implicit E>
            : CCloneableEnumerator<IndexBoundCursor<TColl, TIdx, TElem>, TElem>
            where I : CIndexable<TColl, TIdx, TElem>
            where N : Num<TIdx>
            where E : Eq<TIdx>
        {
            IndexBoundCursor<TColl, TIdx, TElem> Clone(ref this IndexBoundCursor<TColl, TIdx, TElem> e) =>
                new IndexBoundCursor<TColl, TIdx, TElem>
                {
                    container = e.container,
                    pos = N.FromInteger(-1),
                    len = e.len
                };

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
        public instance Enumerable_IndexBound<TColl, [AssociatedType]TIdx, [AssociatedType]TElem, implicit I, implicit N, implicit E, implicit L>
            : CEnumerable<TColl, IndexBoundCursor<TColl, TIdx, TElem>>
            where I : CIndexable<TColl, TIdx, TElem>
            where N : Num<TIdx>
            where E : Eq<TIdx>
            where L : CStaticCountable<TColl>
        {
            IndexBoundCursor<TColl, TIdx, TElem> GetEnumerator(TColl container) =>
                new IndexBoundCursor<TColl, TIdx, TElem> { container = container, len = N.FromInteger(L.Count(container)), pos = N.FromInteger(-1) };
        }

        #endregion Enumerables from length and bound
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
        public instance Enumerator_ArrayCursor<TElem> : CCloneableEnumerator<ArrayCursor<TElem>, TElem>
        {
            ArrayCursor<TElem> Clone(ref this ArrayCursor<TElem> e) =>
                new ArrayCursor<TElem>
                {
                    source = e.source,
                    lo = -1,
                    hi = e.hi
                };

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
        public instance Enumerator_List<TElem> : CResettableEnumerator<List<TElem>.Enumerator, TElem>
        {
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
