using System;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    concept CWhere<T, S, [AssociatedType] D>
    {
        D Where(S src, Func<T, bool> f);
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
        public TEnum source;
        public Func<TElem, bool> filter;
        public TElem current;
    }

    /// <summary>
    /// Enumerator instance for <see cref="Where{TEnum, TElem, TProj}"/>.
    /// </summary>
    public instance Enumerator_Where<TEnum, [AssociatedType] TElem, implicit E>
        : CEnumerator<TElem, Where<TEnum, TElem>>
        where E : CEnumerator<TElem, TEnum>
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
    public instance Where_Enumerator<TElem, TEnum, implicit E>
        : CWhere<TElem, TEnum, Where<TEnum, TElem>>
        where E : CEnumerator<TElem, TEnum>
    {
        Where<TEnum, TElem> Where(TEnum e, Func<TElem, bool> filter) => new Where<TEnum, TElem> { source = e, filter = filter, current = default };
    }

    /// <summary>
    /// Unspecialised instance for filtering over an enumerable, producing
    /// a basic <see cref="Where{TEnum, TElem}"/>.
    /// </summary>
    [Overlappable]
    public instance Where_Enumerable<TElem, TSrc, [AssociatedType] TEnum, implicit E>
        : CWhere<TElem, TSrc, Where<TEnum, TElem>>
        where E : CEnumerable<TSrc, TElem, TEnum>
    {
        Where<TEnum, TElem> Where(TSrc src, Func<TElem, bool> filter) => new Where<TEnum, TElem> { source = E.GetEnumerator(src), filter = filter, current = default };
    }
}
