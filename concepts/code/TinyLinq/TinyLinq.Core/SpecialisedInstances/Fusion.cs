using System;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq.SpecialisedInstances
{
    // Specialised instances for executing fused queries.
    //
    // These reduce various query patterns into smaller structs.

    #region Select of Select

    /// <summary>
    /// Instance reducing chained unspecialised Select queries to a single
    /// <see cref="Select{TEnum, TElem, TProj}"/> on a composed projection.
    /// </summary>
    public instance Select_Select<TElem, TProj1, TProj2, TDest> : CSelect<TProj1, TProj2, Select<TDest, TElem, TProj1>, Select<TDest, TElem, TProj2>>
    {
        Select<TDest, TElem, TProj2> Select(this Select<TDest, TElem, TProj1> t, Func<TProj1, TProj2> projection) =>
            new Select<TDest, TElem, TProj2>
            {
                source = t.source,
                projection = x => projection(t.projection(x)),
                current = default
            };
    }

    #endregion Select of Select

    #region Where of Select

    /// <summary>
    /// Fused Where query on an unspecialised Select.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the enumerator being selected over.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of elements leaving <typeparamref name="TEnum"/>.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements being selected and filtered.
    /// </typeparam>
    public struct WhereOfSelect<TEnum, TElem, TProj>
    {
        /// <summary>
        /// The source enumerator.
        /// </summary>
        public TEnum source;
        /// <summary>
        /// The projection function from the Select.
        /// </summary>
        public Func<TElem, TProj> projection;
        /// <summary>
        /// The filtering predicate from the Where.
        /// </summary>
        public Func<TProj, bool> filter;
        /// <summary>
        /// The cached current item.
        /// </summary>
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for fused Wheres on unspecialised Selects.
    /// </summary>
    public instance Enumerator_WhereSelect<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<WhereOfSelect<TEnum, TElem, TProj>, TProj>
        where E : CEnumerator<TEnum, TElem>
    {
        void Reset(ref WhereOfSelect<TEnum, TElem, TProj> enumerator) => E.Reset(ref enumerator.source);

        bool MoveNext(ref WhereOfSelect<TEnum, TElem, TProj> enumerator)
        {
            do
            {
                if (!E.MoveNext(ref enumerator.source))
                {
                    return false;
                }
                enumerator.current = enumerator.projection(E.Current(ref enumerator.source));
            } while (!enumerator.filter(enumerator.current));

            return true;
        }

        TProj Current(ref WhereOfSelect<TEnum, TElem, TProj> enumerator) => enumerator.current;

        void Dispose(ref WhereOfSelect<TEnum, TElem, TProj> enumerator) => E.Dispose(ref enumerator.source);
    }

    /// <summary>
    /// Instance reducing a Where on a Select to a single composed
    /// qyery.
    /// </summary>
    public instance Where_Select<TEnum, TElem, TProj> : CWhere<Select<TEnum, TElem, TProj>, TProj, WhereOfSelect<TEnum, TElem, TProj>>
    {
        WhereOfSelect<TEnum, TElem, TProj> Where(this Select<TEnum, TElem, TProj> selection, Func<TProj, bool> filter) =>
            new WhereOfSelect<TEnum, TElem, TProj>
            {
                source = selection.source,
                projection = selection.projection,
                filter = filter,
                current = default
            };
    }

    #endregion Where of Select

    #region Select of Where

    /// <summary>
    /// Fused Select query on an unspecialised Where.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the enumerator being selected over.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of elements leaving <typeparamref name="TEnum"/>.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of elements being selected and filtered.
    /// </typeparam>
    public struct SelectOfWhere<TEnum, TElem, TProj>
    {
        /// <summary>
        /// The original source enumerator.
        /// </summary>
        public TEnum source;
        /// <summary>
        /// The filtering predicate from the Where query.
        /// </summary>
        public Func<TElem, bool> filter;
        /// <summary>
        /// The projection function from the Select query.
        /// </summary>
        public Func<TElem, TProj> projection;
        /// <summary>
        /// The cached current item.
        /// </summary>
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for fused Selects on unspecialised Wheres.
    /// </summary>
    public instance Enumerator_SelectWhere<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<SelectOfWhere<TEnum, TElem, TProj>, TProj>
        where E : CEnumerator<TEnum, TElem>
    {
        void Reset(ref SelectOfWhere<TEnum, TElem, TProj> sw) => E.Reset(ref sw.source);

        bool MoveNext(ref SelectOfWhere<TEnum, TElem, TProj> sw)
        {
            TElem c;
            ref var s = ref sw.source;

            do
            {
                if (!E.MoveNext(ref s))
                {
                    return false;
                }
                c = E.Current(ref s);
            } while (!sw.filter(c));

            sw.current = sw.projection(c);
            return true;
        }

        TProj Current(ref SelectOfWhere<TEnum, TElem, TProj> sw) => sw.current;

        void Dispose(ref SelectOfWhere<TEnum, TElem, TProj> sw) => E.Dispose(ref sw.source);
    }

    /// <summary>
    /// Instance reducing a Select on a Where to a single composed
    /// query.
    /// </summary>
    public instance Select_Where<TElem, TProj, TDest> : CSelect<TElem, TProj, Where<TDest, TElem>, SelectOfWhere<TDest, TElem, TProj>>
    {
        SelectOfWhere<TDest, TElem, TProj> Select(this Where<TDest, TElem> t, Func<TElem, TProj> projection) =>
            new SelectOfWhere<TDest, TElem, TProj>
            {
                source = t.source,
                filter = t.filter,
                projection = projection,
                current = default
            };
    }

    #endregion Select of Where
}
