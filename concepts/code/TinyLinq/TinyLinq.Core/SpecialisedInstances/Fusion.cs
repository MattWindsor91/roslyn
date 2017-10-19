using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;

namespace TinyLinq.SpecialisedInstances
{
    // Specialised instances for executing fused queries.
    //
    // These reduce various query patterns into smaller structs.

    #region Select of Select

    /// <summary>Fusion of two Select queries.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TFused"> Type of fused elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Select_Select<TSourceColl, TSourceEnum, TSource, TFused, TResult>
        : CSelect<SelectCursor<TSourceColl, TSourceEnum, TSource, TFused>, TFused, TResult, SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>>
    {
        SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> Select(this SelectCursor<TSourceColl, TSourceEnum, TSource, TFused> c, Func<TFused, TResult> selector) =>
            new SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>(c.source, x => selector(c.selector(x)));
    }

    #endregion Select of Select

    #region Where of Select

    /// <summary>Cursor representing a fused Where-of-Select query.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult>
    {
        /// <summary>The selector function.</summary>
        public readonly Func<TSource, TResult> selector;
        /// <summary>The filtering predicate.</summary>
        public readonly Func<TResult, bool> predicate;
        /// <summary> The source collection.</summary>
        public readonly TSourceColl source;

        /// <summary>The state of this cursor.</summary>
        public CursorState state;
        /// <summary>The source enumerator, lazily fetched.</summary>
        public TSourceEnum sourceEnum;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>Constructs a new Where-of-Select cursor.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="selector">The selector function.</param>
        /// <param name="predicate">The predicate function.</param>
        public WhereOfSelectCursor(TSourceColl source, Func<TSource, TResult> selector, Func<TResult, bool> predicate)
        {
            this.selector = selector;
            this.predicate = predicate;
            this.source = source;

            state = CursorState.Uninitialised;
            sourceEnum = default;
            result = default;
        }
    }

    /// <summary>Where-Of-Select cursors are cloneable enumerators.</summary>
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
    public instance Enumerator_WhereOfSelectCursor<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType]TSource, TResult, implicit Eb, implicit Et>
        : CCloneableEnumerator<WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult>, TResult>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> Clone(ref this WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c) =>
            new WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult>(c.source, c.selector, c.predicate);

        void Reset(ref WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                Et.Dispose(ref c.sourceEnum);
                c.sourceEnum = default;
                c.result = default;
            }
            c.state = CursorState.Uninitialised;
        }

        bool MoveNext(ref WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
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
                        c.result = c.selector(Et.Current(ref c.sourceEnum));
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

        TResult Current(ref WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c) => c.result;

        void Dispose(ref WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                Et.Dispose(ref c.sourceEnum);
            }
        }
    }

    /// <summary>Where-Of-Select cursors are countable.</summary>
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
    public instance Countable_WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult, implicit Eb, implicit Et>
        : CCountable<WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult>>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        int Count(this WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            var e = c.source.GetEnumerator();
            var count = 0;

            while (Et.MoveNext(ref e))
            {
                var current = c.selector(Et.Current(ref e));
                if (c.predicate(current))
                {
                    count++;
                }
            }

            Et.Dispose(ref e);
            return count;
        }
    }

    /// <summary>Fusion of Where on Select.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Where_Select<TSourceColl, TSourceEnum, TSource, TResult>
        : CWhere<SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>, TResult, WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult>>
    {
        WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult> Where(this SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> source, Func<TResult, bool> predicate) =>
            new WhereOfSelectCursor<TSourceColl, TSourceEnum, TSource, TResult>(source.source, source.selector, predicate);
    }

    #endregion Where of Select

    #region Select of Where

    /// <summary>Cursor representing a fused Select-of-Where query.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult>
    {
        /// <summary>The filtering predicate.</summary>
        public readonly Func<TSource, bool> predicate;
        /// <summary>The selector function.</summary>
        public readonly Func<TSource, TResult> selector;
        /// <summary> The source collection.</summary>
        public readonly TSourceColl source;

        /// <summary>The state of this cursor.</summary>
        public CursorState state;
        /// <summary>The source enumerator, lazily fetched.</summary>
        public TSourceEnum sourceEnum;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>Constructs a new Where-of-Select cursor.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="predicate">The predicate function.</param>
        /// <param name="selector">The selector function.</param>
        public SelectOfWhereCursor(TSourceColl source, Func<TSource, bool> predicate, Func<TSource, TResult> selector)
        {
            this.predicate = predicate;
            this.selector = selector;
            this.source = source;

            state = CursorState.Uninitialised;
            sourceEnum = default;
            result = default;
        }
    }

    /// <summary>Select-of-Where cursors are cloneable enumerators.</summary>
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
    public instance Enumerator_SelectOfWhereCursor<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType]TSource, TResult, implicit Eb, implicit Et>
        : CCloneableEnumerator<SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult>, TResult>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> Clone(ref this SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> c) =>
            new SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult>(c.source, c.predicate, c.selector);

        void Reset(ref SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                Et.Dispose(ref c.sourceEnum);
                c.sourceEnum = default;
                c.result = default;
            }
            c.state = CursorState.Uninitialised;
        }

        bool MoveNext(ref SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
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
                        var tmp = Et.Current(ref c.sourceEnum);
                        if (c.predicate(tmp))
                        {
                            c.result = c.selector(tmp);
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

        TResult Current(ref SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> c) => c.result;

        void Dispose(ref SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                Et.Dispose(ref c.sourceEnum);
            }
        }
    }

    /// <summary>Select-of-Where cursors are countable.</summary>
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
    public instance Countable_SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult, implicit Eb, implicit Et>
        : CCountable<SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult>>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        int Count(this SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            var e = c.source.GetEnumerator();
            var count = 0;

            while (Et.MoveNext(ref e))
            {
                var current = Et.Current(ref e);
                if (c.predicate(current))
                {
                    // Needed to ensure any exceptions or side-effects in the
                    // projection occur.
                    c.selector(current);
                    count++;
                }
            }

            Et.Dispose(ref e);
            return count;
        }
    }

    /// <summary>Fusion of Select on Where.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public instance Select_Where<TSourceColl, TSourceEnum, TSource, TResult>
        : CSelect<WhereCursor<TSourceColl, TSourceEnum, TSource>, TSource, TResult, SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult>>
    {
        SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult> Select(this WhereCursor<TSourceColl, TSourceEnum, TSource> source, Func<TSource, TResult> selector) =>
            new SelectOfWhereCursor<TSourceColl, TSourceEnum, TSource, TResult>(source.source, source.predicate, selector);
    }

    #endregion Select of Where
}
