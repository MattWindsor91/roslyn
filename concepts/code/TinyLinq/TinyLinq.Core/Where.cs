using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>Concept for types that can be Where-queried.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResultColl">Type of output collection.</typeparam>
    public concept CWhere<TSourceColl, [AssociatedType]TSource, [AssociatedType]TResultColl>
    {
        /// <summary>Filters a source using the given predicate.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="predicate">The filtering predicate.</param>
        /// <returns>
        /// A lazy query that, when enumerated, returns each item from
        /// <paramref name="source"/> satisfying <paramref name="predicate"/>.
        /// </returns>
        TResultColl Where(this TSourceColl source, Func<TSource, bool> predicate);
    }

    /// <summary>Cursor representing an unspecialised Where.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    public struct WhereCursor<TSourceColl, TSourceEnum, TSource>
    {
        /// <summary>The filtering predicate.</summary>
        public readonly Func<TSource, bool> predicate;
        /// <summary> The source collection.</summary>
        public readonly TSourceColl source;

        /// <summary>The state of this cursor.</summary>
        public CursorState state;
        /// <summary>The source enumerator, lazily fetched.</summary>
        public TSourceEnum sourceEnum;
        /// <summary>The cached current result.</summary>
        public TSource result;

        /// <summary>Constructs a new Select cursor.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="predicate">The predicate function.</param>
        public WhereCursor(TSourceColl source, Func<TSource, bool> predicate)
        {
            this.predicate = predicate;
            this.source = source;

            state = CursorState.Uninitialised;
            sourceEnum = default;
            result = default;
        }
    }

    /// <summary>Wellformed Where cursors are cloneable enumerators.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    /// <typeparam name="Eb">
    /// Instance of <see cref="CEnumerable{TColl, TEnum}"/> for
    /// <typeparamref name="TSourceColl"/>.
    /// </typeparam>
    /// <typeparam name="Et">
    /// Instance of <see cref="CEnumerator{TEnum, TElem}"/> for
    /// <typeparamref name="TSourceEnum"/>.
    /// </typeparam>
    public instance Enumerator_WhereCursor<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType] TSource, implicit Eb, implicit Et>
        : CCloneableEnumerator<WhereCursor<TSourceColl, TSourceEnum, TSource>, TSource>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        WhereCursor<TSourceColl, TSourceEnum, TSource> Clone(ref this WhereCursor<TSourceColl, TSourceEnum, TSource> c) =>
            new WhereCursor<TSourceColl, TSourceEnum, TSource>(c.source, c.predicate);

        void Reset(ref this WhereCursor<TSourceColl, TSourceEnum, TSource> c)
        {
            if (c.state == CursorState.Active)
            {
                Et.Dispose(ref c.sourceEnum);
                c.sourceEnum = default;
                c.result = default;
            }
            c.state = CursorState.Uninitialised;
        }

        bool MoveNext(ref WhereCursor<TSourceColl, TSourceEnum, TSource> c)
        {
            switch (c.state)
            {
                case CursorState.Exhausted:
                    return false;
                case CursorState.Uninitialised:
                    c.sourceEnum = c.source.GetEnumerator();
                    c.state = CursorState.Active;
                    goto case CursorState.Active;
                case CursorState.Active:
                    while (Et.MoveNext(ref c.sourceEnum))
                    {
                        c.result = Et.Current(ref c.sourceEnum);
                        if (c.predicate(c.result))
                        {
                            return true;
                        }
                    }

                    Et.Dispose(ref c.sourceEnum);
                    c.sourceEnum = default;
                    c.result = default;
                    c.state = CursorState.Exhausted;
                    return false;
                default:
                    return false;
            }
        }

        TSource Current(ref WhereCursor<TSourceColl, TSourceEnum, TSource> c) => c.result;

        void Dispose(ref WhereCursor<TSourceColl, TSourceEnum, TSource> c)
        {
            if (c.state == CursorState.Active)
            {
                Et.Dispose(ref c.sourceEnum);
            }
        }
    }

    /// <summary>
    /// Instance for O(n) counting of Wheres.
    /// </summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="Eb">
    /// Instance of <see cref="CEnumerable{TColl, TEnum}"/> for
    /// <typeparamref name="TSourceColl"/>.
    /// </typeparam>
    /// <typeparam name="Et">
    /// Instance of <see cref="CEnumerator{TEnum, TElem}"/> for
    /// <typeparamref name="TSourceEnum"/>.
    /// </typeparam>
    public instance Countable_WhereCursor<TSourceColl, TSourceEnum, TSource, implicit Eb, implicit Et>
        : CCountable<WhereCursor<TSourceColl, TSourceEnum, TSource>>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        int Count(this WhereCursor<TSourceColl, TSourceEnum, TSource> c)
        {
            var e = c.source.GetEnumerator();

            var count = 0;
            while (Et.MoveNext(ref e))
            {
                if (c.predicate(Et.Current(ref e)))
                {
                    count++;
                }
            }

            Et.Dispose(ref e);
            return count;
        }
    }

    /// <summary>
    /// Unspecialised instance for filtering over an enumerator, producing
    /// a basic <see cref="WhereCursor{TSourceColl, TSourceEnum, TSource}"/>.
    /// </summary>
    [Overlappable]
    public instance Where_Enumerable<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType]TSource, implicit Eb, implicit Et>
        : CWhere<TSourceColl, TSource, WhereCursor<TSourceColl, TSourceEnum, TSource>>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        WhereCursor<TSourceColl, TSourceEnum, TSource> Where(this TSourceColl source, Func<TSource, bool> predicate) =>
            new WhereCursor<TSourceColl, TSourceEnum, TSource>(source, predicate);
    }
}
