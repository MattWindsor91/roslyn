using System;
using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>Concept for types that can be SelectMany-queried.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TCollectionColl">Type of inner collection.</typeparam>
    /// <typeparam name="TCollection"> Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    /// <typeparam name="TResultColl">Type of output collection.</typeparam>
    public concept CSelectMany<TSourceColl, [AssociatedType]TSource, TCollectionColl, [AssociatedType]TCollection, TResult, [AssociatedType]TResultColl>
    {
        /// <summary>Creates a SelectMany query on a source.</summary>
        /// <param name="source">The source collection to query.</param>
        /// <param name="collectionSelector">The collection selector.</param>
        /// <param name="resultSelector">The result selector.</param>
        /// <returns>
        /// A lazy query that, when enumerated, uses
        /// <paramref name="collectionSelector"/> to select a collection for
        /// each element of <paramref name="source"/>, then returns the
        /// transformation of each item in each such collection, and its
        /// parent source element, according to
        /// <paramref name="resultSelector"/>.
        /// </returns>
        TResultColl SelectMany(this TSourceColl source, Func<TSource, TCollectionColl> collectionSelector, Func<TSource, TCollection, TResult> resultSelector);
    }

    /// <summary>Cursor representing an unspecialised SelectMany.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TCollectionColl">Type of inner collection.</typeparam>
    /// <typeparam name="TCollectionEnum">Type of inner enumerator.</typeparam>
    /// <typeparam name="TCollection"> Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    public struct SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult>
    {
        /// <summary>The collection selector.</summary>
        public readonly Func<TSource, TCollectionColl> collectionSelector;
        /// <summary>The result selector.</summary>
        public readonly Func<TSource, TCollection, TResult> resultSelector;
        /// <summary> The source collection.</summary>
        public TSourceColl source;

        /// <summary>The state of this cursor.</summary>
        public CursorState state;
        /// <summary>The source enumerator, lazily fetched.</summary>
        public TSourceEnum sourceEnum;
        /// <summary>The current source element.</summary>
        public TSource sourceElem;
        /// <summary>The current collection enumerator.</summary>
        public TCollectionEnum collectionEnum;
        /// <summary>The cached current result.</summary>
        public TResult result;

        /// <summary>
        /// Creates a new array-to-array SelectMany cursor.
        /// </summary>
        /// <param name="source">The source collection.</param>
        /// <param name="collectionSelector">The collection selector.</param>
        /// <param name="resultSelector">The result selector.</param>  
        public SelectManyCursor(TSourceColl source, Func<TSource, TCollectionColl> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            this.source = source;
            this.collectionSelector = collectionSelector;
            this.resultSelector = resultSelector;

            state = CursorState.Uninitialised;
            sourceEnum = default;
            sourceElem = default;
            collectionEnum = default;
            result = default;
        }
    }

    /// <summary>SelectMany cursors are cloneable enumerators.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TCollectionColl">Type of inner collection.</typeparam>
    /// <typeparam name="TCollectionEnum">Type of inner enumerator.</typeparam>
    /// <typeparam name="TCollection"> Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    /// <typeparam name="SEb">
    /// Instance of <see cref="CEnumerable{TColl, TEnum}"/> for
    /// <typeparamref name="TSourceColl"/>.
    /// </typeparam>
    /// <typeparam name="SEt">
    /// Instance of <see cref="CEnumerator{TEnum, TElem}"/> for
    /// <typeparamref name="TSourceEnum"/>.
    /// </typeparam>
    /// <typeparam name="CEb">
    /// Instance of <see cref="CEnumerable{TColl, TEnum}"/> for
    /// <typeparamref name="TCollectionColl"/>.
    /// </typeparam>
    /// <typeparam name="CEt">
    /// Instance of <see cref="CEnumerator{TEnum, TElem}"/> for
    /// <typeparamref name="TCollectionEnum"/>.
    /// </typeparam>
    public instance Enumerator_SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult, implicit SEb, implicit SEt, implicit CEb, implicit CEt>
        : CCloneableEnumerator<SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult>, TResult>
        where SEb : CEnumerable<TSourceColl, TSourceEnum>
        where SEt : CEnumerator<TSourceEnum, TSource>
        where CEb : CEnumerable<TCollectionColl, TCollectionEnum>
        where CEt : CEnumerator<TCollectionEnum, TCollection>
    {
        SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> Clone(ref this SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> c) =>
            new SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult>(c.source, c.collectionSelector, c.resultSelector);

        void Reset(ref this SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                c.sourceEnum.Dispose();
                c.collectionEnum.Dispose();
                c.sourceEnum = default;
                c.collectionEnum = default;
                c.sourceElem = default;
                c.result = default;
            }
            c.state = CursorState.Uninitialised;
        }

        bool MoveNext(ref this SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> c)
        {
            switch (c.state)
            {
                case CursorState.Exhausted:
                    return false;
                case CursorState.Uninitialised:
                    // Need to put *both* enumerators into stable states.
                    c.sourceEnum = c.source.RefGetEnumerator();
                    if (!c.sourceEnum.MoveNext())
                    {
                        goto sourceOut;
                    }
                    c.sourceElem = c.sourceEnum.Current();
                    c.collectionEnum = c.collectionSelector(c.sourceElem).GetEnumerator();
                    c.state = CursorState.Active;
                    goto case CursorState.Active;
                case CursorState.Active:
                    while (true)
                    {
                        // Stable state: we have a collection; it may be empty.
                        if (c.collectionEnum.MoveNext())
                        {
                            c.result = c.resultSelector(c.sourceElem, c.collectionEnum.Current());
                            return true;
                        }
                        // Collection empty, try get a new one from the source.
                        c.collectionEnum.Dispose();
                        if (!c.sourceEnum.MoveNext())
                        {
                            goto sourceOut;
                        }
                        c.sourceElem = c.sourceEnum.Current();
                        c.collectionEnum = c.collectionSelector(c.sourceElem).GetEnumerator();
                        // Now loop back to try this next collection.
                    }
                default:
                    return false;
            }

            sourceOut:
            c.sourceEnum.Dispose();
            c.sourceEnum = default;
            c.result = default;
            c.state = CursorState.Exhausted;
            return false;
        }

        TResult Current(ref this SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> c) => c.result;

        void Dispose(ref this SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> c)
        {
            if (c.state == CursorState.Active)
            {
                c.sourceEnum.Dispose();
                c.collectionEnum.Dispose();
            }
        }
    }

    /// <summary>Unspecialised SelectMany over enumerables.</summary>
    /// <typeparam name="TSourceColl">Type of source collection.</typeparam>
    /// <typeparam name="TSourceEnum">Type of source enumerator.</typeparam>
    /// <typeparam name="TSource"> Type of source elements.</typeparam>
    /// <typeparam name="TCollectionColl">Type of inner collection.</typeparam>
    /// <typeparam name="TCollectionEnum">Type of inner enumerator.</typeparam>
    /// <typeparam name="TCollection"> Type of inner elements.</typeparam>
    /// <typeparam name="TResult">Type of query results.</typeparam>
    /// <typeparam name="SEb">
    /// Instance of <see cref="CEnumerable{TColl, TEnum}"/> for
    /// <typeparamref name="TSourceColl"/>.
    /// </typeparam>
    /// <typeparam name="SEt">
    /// Instance of <see cref="CEnumerator{TEnum, TElem}"/> for
    /// <typeparamref name="TSourceEnum"/>.
    /// </typeparam>
    /// <typeparam name="CEb">
    /// Instance of <see cref="CEnumerable{TColl, TEnum}"/> for
    /// <typeparamref name="TCollectionColl"/>.
    /// </typeparam>
    /// <typeparam name="CEt">
    /// Instance of <see cref="CEnumerator{TEnum, TElem}"/> for
    /// <typeparamref name="TCollectionEnum"/>.
    /// </typeparam>
    [Overlappable]
    public instance SelectMany_Enumerable<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType]TSource, TCollectionColl, [AssociatedType]TCollectionEnum, [AssociatedType]TCollection, TResult, implicit SEb, implicit SEt, implicit CEb, implicit CEt>
        : CSelectMany<TSourceColl, TSource, TCollectionColl, TCollection, TResult, SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult>>
        where SEb : CEnumerable<TSourceColl, TSourceEnum>
        where SEt : CEnumerator<TSourceEnum, TSource>
        where CEb : CEnumerable<TCollectionColl, TCollectionEnum>
        where CEt : CEnumerator<TCollectionEnum, TCollection>
    {
        SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult> SelectMany(this TSourceColl source, Func<TSource, TCollectionColl> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
            => new SelectManyCursor<TSourceColl, TSourceEnum, TSource, TCollectionColl, TCollectionEnum, TCollection, TResult>(source, collectionSelector, resultSelector);
    }
}
