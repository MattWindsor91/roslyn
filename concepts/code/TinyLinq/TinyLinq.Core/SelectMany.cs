using System;
using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    /// <summary>
    /// Concept for selecting multiple items from each element of a collection,
    /// then projecting them into a new enumerator.
    /// </summary>
    /// <typeparam name="TSrc">
    /// Type of the collection to target.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the elements of <typeparamref name="TSrc"/>.
    /// </typeparam>
    /// <typeparam name="TInner">
    /// Type of the collection returned by the first projection.
    /// </typeparam>
    /// <typeparam name="TInnerElem">
    /// Type of the elements of <typeparamref name="TInner"/>.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of the elements returned by the second projection.
    /// </typeparam>
    /// <typeparam name="TDest">
    /// Type of the enumerator over <typeparamref name="TInner"/>
    /// that the selection returns.
    /// </typeparam>
    public concept CSelectMany<TSrc, [AssociatedType] TElem, TInner, [AssociatedType] TInnerElem, [AssociatedType] TProj, [AssociatedType] TDest>
    {
        TDest SelectMany(this TSrc src, Func<TElem, TInner> selector, Func<TElem, TInnerElem, TProj> resultSelector);
    }

    public struct SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj>
    {
        public TSrc source;
        public TElem currentElem;
        public TInnerSrc currentInnerSource;

        public TProj current;
        public bool started;
        public bool finished;

        public Func<TElem, TInnerColl> outerProjection;
        public Func<TElem, TInnerElem, TProj> innerProjection;
    }

    public instance Enumerator_SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj, implicit ES, implicit EI>
        : CEnumerator<SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj>, TProj>
            where ES : CEnumerator<TSrc, TElem>
            where EI : CEnumerable<TInnerColl, TInnerSrc, TInnerElem>
    {
        void Reset(ref SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj> sm)
        {
            EI.Dispose(ref sm.currentInnerSource);
            sm.started = sm.finished = false;

            ES.Reset(ref sm.source);
        }

        bool MoveNext(ref SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj> sm)
        {
            // Outer enumerator has finished: we're done.
            if (sm.finished)
            {
                return false;
            }

            // Outer enumerator hasn't started yet: make sure we do so first.
            var mustCycleOuter = !sm.started;
            sm.started = true;

            // Keep going until we run out of outer enumerators.
            while (true)
            {
                if (mustCycleOuter)
                {
                    if (!ES.MoveNext(ref sm.source))
                    {
                        // We've run out of outer enumerators, so we're done.
                        sm.finished = true;
                        return false;
                    }
                    sm.currentElem = ES.Current(ref sm.source);
                    sm.currentInnerSource = EI.GetEnumerator(sm.outerProjection(sm.currentElem));
                }
                mustCycleOuter = true;

                // Does the inner enumerator have something left in it?
                if (EI.MoveNext(ref sm.currentInnerSource))
                {
                    sm.current = sm.innerProjection(sm.currentElem, EI.Current(ref sm.currentInnerSource));
                    return true;
                }

                // It doesn't, so move the outer enumerator.
                EI.Dispose(ref sm.currentInnerSource);
            }
        }

        TProj Current(ref SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj> sm) => sm.current;

        void Dispose(ref SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj> sm)
        {
            if (sm.started && !sm.finished)
            {
                EI.Dispose(ref sm.currentInnerSource);
            }
            ES.Dispose(ref sm.source);
        }
    }

    [Overlappable]
    public instance SelectMany_Enumerator<TSrc, TElem, TInnerColl, [AssociatedType] TInnerSrc, TInnerElem, TProj, implicit EI, implicit ES>
        : CSelectMany<TSrc, TElem, TInnerColl, TInnerElem, TProj, SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj>>
        where ES : CEnumerator<TSrc, TElem>
        where EI : CEnumerable<TInnerColl, TInnerSrc, TInnerElem>
    {
        SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj> SelectMany(this TSrc src, Func<TElem, TInnerColl> outerProj, Func<TElem, TInnerElem, TProj> innerProj)
            => new SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj>
            {
                source = src,
                started = false,
                finished = false,
                outerProjection = outerProj,
                innerProjection = innerProj
            };
    }

    [Overlappable]
    public instance SelectMany_Enumerable<TColl, [AssociatedType]TSrc, [AssociatedType]TElem, TInnerColl, [AssociatedType] TInnerSrc, [AssociatedType]TInnerElem, TProj, implicit EI, implicit ES>
        : CSelectMany<TColl, TElem, TInnerColl, TInnerElem, TProj, SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj>>
        where ES : CEnumerable<TColl, TSrc, TElem>
        where EI : CEnumerable<TInnerColl, TInnerSrc, TInnerElem>
    {
        SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj> SelectMany(this TColl coll, Func<TElem, TInnerColl> outerProj, Func<TElem, TInnerElem, TProj> innerProj)
            => new SelectMany<TSrc, TElem, TInnerColl, TInnerSrc, TInnerElem, TProj>
            {
                source = ES.GetEnumerator(coll),
                started = false,
                finished = false,
                outerProjection = outerProj,
                innerProjection = innerProj
            };
    }
}
