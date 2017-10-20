using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>Concept for types that can be Select-queried.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    /// <typeparam name="TResultColl">Type of output collection.</typeparam>
    public concept CSelect<TSourceColl, [AssociatedType]TSource, TResult, [AssociatedType]TResultColl>
    {
        /// <summary>Selects on a source using the given selector.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="selector">The selector function.</param>
        /// <returns>
        /// A lazy query that, when enumerated, returns each item from
        /// <paramref name="source"/> after transformation by
        /// <paramref name="selector"/>.
        /// </returns>
        TResultColl Select(this TSourceColl source, Func<TSource, TResult> selector);
    }

    /// <summary>Cursor representing an unspecialised Select.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>
    {
        /// <summary>The selector function.</summary>
        public readonly Func<TSource, TResult> selector;
        /// <summary> The source collection.</summary>
        public TSourceColl source;

        /// <summary>The state of this cursor.</summary>
        public CursorState state;
        /// <summary>The source enumerator, lazily fetched.</summary>
        public TSourceEnum sourceEnum;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>Constructs a new Select cursor.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="selector">The selector function.</param>
        public SelectCursor(TSourceColl source, Func<TSource, TResult> selector)
        {
            this.selector = selector;
            this.source = source;

            state = CursorState.Uninitialised;
            sourceEnum = default;
            result = default;
        }
    }

    /// <summary>Wellformed Select cursors are cloneable enumerators.</summary>
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
    public instance Enumerator_Select<TSourceColl, TSourceEnum, TSource, TResult, implicit Eb, implicit Et>
        : CCloneableEnumerator<SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>, TResult>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> Clone(ref SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c) =>
            new SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>(c.source, c.selector);

        void Reset(ref this SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                c.sourceEnum.Dispose();
                c.sourceEnum = default;
                c.result = default;
            }
            c.state = CursorState.Uninitialised;
        }

        bool MoveNext(ref SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            switch (c.state)
            {
                case CursorState.Exhausted:
                    return false;
                case CursorState.Uninitialised:
                    c.sourceEnum = c.source.RefGetEnumerator();
                    c.state = CursorState.Active;
                    goto case CursorState.Active;
                case CursorState.Active:
                    if (c.sourceEnum.MoveNext())
                    {
                        c.result = c.selector(c.sourceEnum.Current());
                        return true;
                    }

                    c.sourceEnum.Dispose();
                    c.sourceEnum = default;
                    c.result = default;
                    c.state = CursorState.Exhausted;
                    return false;
                default:
                    return false;
            }
        }

        TResult Current(ref SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c) => c.result;

        void Dispose(ref SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                c.sourceEnum.Dispose();
            }
        }
    }

    /// <summary>
    /// Instance for O(n) counting of Selects.
    /// <para>
    /// Counting evaluates the selector on each value.
    /// </para>
    /// </summary>
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
    public instance Countable_SelectCursor<TSourceColl, TSourceEnum, TSource, TResult, implicit Eb, implicit Et>
        : CCountable<SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        // NOTE: If we knew that the selector was pure, we could also make a
        //       CStaticCountable instance for Select.  Unfortunately, we
        //       have no way of knowing this.

        int Count(this SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> c)
        {
            var e = c.source.RefGetEnumerator();

            var count = 0;
            while (e.MoveNext())
            {
                // The selector might be impure, so we need to let it run.
                c.selector(e.Current());
                count++;
            }

            e.Dispose();
            return count;
        }
    }

    /// <summary>Unspecialised Select over an enumerable.</summary>
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
    [Overlappable]
    public instance Select_Basic<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType]TSource, TResult, implicit Eb, implicit Et>
        : CSelect<TSourceColl, TSource, TResult, SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
    {
        SelectCursor<TSourceColl, TSourceEnum, TSource, TResult> Select(this TSourceColl source, Func<TSource, TResult> selector) =>
            new SelectCursor<TSourceColl, TSourceEnum, TSource, TResult>(source, selector);
    }
}
