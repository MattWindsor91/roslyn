using System;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    public concept CSelect<[AssociatedType] T, [AssociatedType] U, S, [AssociatedType] D>
    {
        D Select(this S src, Func<T, U> f);
    }

    /// <summary>
    /// Enumerator representing an unspecialised Select.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the enumerator we are selecting over.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the element <typeparamref name="TEnum"/> returns.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of the projected element the selection returns.
    /// </typeparam>
    public struct Select<TEnum, TElem, TProj>
    {
        public TEnum source;
        public Func<TElem, TProj> projection;
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for <see cref="Select{TEnum, TElem, TProj}"/>.
    /// </summary>
    public instance Enumerator_Select<TEnum, TElem, TProj, implicit E>
        : CEnumerator<Select<TEnum, TElem, TProj>, TProj>
        where E : CEnumerator<TEnum, TElem>
    {
        void Reset(ref Select<TEnum, TElem, TProj> s) => E.Reset(ref s.source);

        bool MoveNext(ref Select<TEnum, TElem, TProj> s)
        {
            if (!E.MoveNext(ref s.source))
            {
                return false;
            }

            s.current = s.projection(E.Current(ref s.source));
            return true;
        }

        TProj Current(ref Select<TEnum, TElem, TProj> s) => s.current;

        void Dispose(ref Select<TEnum, TElem, TProj> s) { }
    }

    /// <summary>
    /// Unspecialised instance for selecting over an enumerator, producing
    /// a basic <see cref="Select{TEnum, TElem, TProj}"/>.
    /// </summary>
    [Overlappable]
    public instance Select_Enumerator<TEnum, TElem, TProj, implicit E>
        : CSelect<TElem, TProj, TEnum, Select<TEnum, TElem, TProj>>
        where E : CEnumerator<TEnum, TElem>
    {
        Select<TEnum, TElem, TProj> Select(this TEnum t, Func<TElem, TProj> projection) =>
            new Select<TEnum, TElem, TProj>
            {
                source = t,
                projection = projection,
                current = default
            };
    }

    /// <summary>
    /// Adapts any selection over enumerators into one over enumerables.
    /// </summary>
    [Overlappable]
    public instance Select_Enumerable<TColl, [AssociatedType] TSrc, [AssociatedType] TElem, TProj, [AssociatedType] TDst, implicit S, implicit E>
        : CSelect<TElem, TProj, TColl, TDst>
        where S : CSelect<TElem, TProj, TSrc, TDst>
        where E : CEnumerable<TColl, TSrc, TElem>
    {
        TDst Select(this TColl t, Func<TElem, TProj> projection) => S.Select(E.GetEnumerator(t), projection);
    }
}
