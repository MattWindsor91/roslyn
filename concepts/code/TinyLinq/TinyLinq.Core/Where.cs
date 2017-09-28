using System;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>
    /// Concept for types that can be filtered by predicate.
    /// </summary>
    /// <typeparam name="TSrc">
    /// The type that can be filtered.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// The type of elements from <typeparamref name="TSrc"/>.
    /// </typeparam>
    /// <typeparam name="TDest">
    /// The type of the output, which will usually be a lazy enumerator over
    /// the filtered elements.
    /// </typeparam>
    public concept CWhere<TSrc, [AssociatedType] TElem, [AssociatedType] TDest>
    {
        TDest Where(this TSrc src, Func<TElem, bool> f);
    }

    /// <summary>
    /// Enumerator representing an unspecialised Where.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the enumerator we are filtering over.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the element <typeparamref name="TEnum"/> returns.
    /// </typeparam>
    public struct Where<TEnum, TElem>
    {
        /// <summary>
        /// The source of the elements being filtered.
        /// </summary>
        public TEnum source;
        /// <summary>
        /// The filtering predicate.
        /// </summary>
        public Func<TElem, bool> filter;
        /// <summary>
        /// The cached current item.
        /// </summary>
        public TElem current;
    }

    /// <summary>
    /// Enumerator instance for <see cref="Where{TEnum, TElem}"/>.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the inner enumerator we are filtering over.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the element <typeparamref name="TEnum"/> returns.
    /// </typeparam>
    /// <typeparam name="E">
    /// Enumerator instance for the inner enumerator.
    /// </typeparam>
    public instance Enumerator_Where<TEnum, [AssociatedType] TElem, implicit E>
        : CEnumerator<Where<TEnum, TElem>, TElem>
        where E : CEnumerator<TEnum, TElem>
    {
        void Reset(ref Where<TEnum, TElem> w) => E.Reset(ref w.source);

        bool MoveNext(ref Where<TEnum, TElem> w)
        {
            do
            {
                if (!E.MoveNext(ref w.source))
                {
                    return false;
                }
                w.current = E.Current(ref w.source);
            } while (!w.filter(w.current));

            return true;
        }

        TElem Current(ref Where<TEnum, TElem> w) => w.current;

        void Dispose(ref Where<TEnum, TElem> w) => E.Dispose(ref w.source);
    }

    /// <summary>
    /// Unspecialised instance for filtering over an enumerator, producing
    /// a basic <see cref="Where{TEnum, TElem}"/>.
    /// </summary>
    [Overlappable]
    public instance Where_Enumerator<TEnum, [AssociatedType] TElem, implicit E>
        : CWhere<TEnum, TElem, Where<TEnum, TElem>>
        where E : CEnumerator<TEnum, TElem>
    {
        Where<TEnum, TElem> Where(this TEnum e, Func<TElem, bool> filter) => new Where<TEnum, TElem> { source = e, filter = filter, current = default };
    }

    /// <summary>
    /// Adapts any Where over enumerators into one over enumerables.
    /// </summary>
    [Overlappable]
    public instance Where_Enumerable<TColl, [AssociatedType] TSrc, [AssociatedType] TElem, [AssociatedType] TDst, implicit S, implicit E>
        : CWhere<TColl, TElem, TDst>
        where S : CWhere<TSrc, TElem, TDst>
        where E : CEnumerable<TColl, TSrc, TElem>
    {
        TDst Where(this TColl t, Func<TElem, bool> filter) => S.Where(E.GetEnumerator(t), filter);
    }
}
