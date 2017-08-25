using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Enumerable;


/*
 What we need to implement.
 
 7.16.3 The query expression pattern
The Query expression pattern establishes a pattern of methods that types can implement to support query expressions. Because query expressions are translated to method invocations by means of a syntactic mapping, types have considerable flexibility in how they implement the query expression pattern. For example, the methods of the pattern can be implemented as instance methods or as extension methods because the two have the same invocation syntax, and the methods can request delegates or expression trees because anonymous functions are convertible to both.
The recommended shape of a generic type C<T> that supports the query expression pattern is shown below. A generic type is used in order to illustrate the proper relationships between parameter and result types, but it is possible to implement the pattern for non-generic types as well.
delegate R Func<T1,R>(T1 arg1);
delegate R Func<T1,T2,R>(T1 arg1, T2 arg2);
class C
{
	public C<T> Cast<T>();
}
class C<T> : C
{
	public C<T> Where(Func<T,bool> predicate);
	public C<U> Select<U>(Func<T,U> selector);
	public C<V> SelectMany<U,V>(Func<T,C<U>> selector,
		Func<T,U,V> resultSelector);
	public C<V> Join<U,K,V>(C<U> inner, Func<T,K> outerKeySelector,
		Func<U,K> innerKeySelector, Func<T,U,V> resultSelector);
	public C<V> GroupJoin<U,K,V>(C<U> inner, Func<T,K> outerKeySelector,
		Func<U,K> innerKeySelector, Func<T,C<U>,V> resultSelector);
	public O<T> OrderBy<K>(Func<T,K> keySelector);
	public O<T> OrderByDescending<K>(Func<T,K> keySelector);
	public C<G<K,T>> GroupBy<K>(Func<T,K> keySelector);
	public C<G<K,E>> GroupBy<K,E>(Func<T,K> keySelector,
		Func<T,E> elementSelector);
}
class O<T> : C<T>
{
	public O<T> ThenBy<K>(Func<T,K> keySelector);
	public O<T> ThenByDescending<K>(Func<T,K> keySelector);
}
class G<K,T> : C<T>
{
	public K Key { get; }
}
The methods above use the generic delegate types Func<T1, R> and Func<T1, T2, R>, but they could equally well have used other delegate or expression tree types with the same relationships in parameter and result types.
Notice the recommended relationship between C<T> and O<T> which ensures that the ThenBy and ThenByDescending methods are available only on the result of an OrderBy or OrderByDescending. Also notice the recommended shape of the result of GroupBy—a sequence of sequences, where each inner sequence has an additional Key property.
The System.Linq namespace provides an implementation of the query operator pattern for any type that implements the System.Collections.Generic.IEnumerable<T> interface.

 */

namespace TinyLinq
{
    public concept CSelect<[AssociatedType] T, [AssociatedType] U, S, D>
    {
        D Select(S src, Func<T, U> f);
    }

    public struct Selection<TEnum, TElem, TProj>
    {
        public TEnum source;
        public Func<TElem, TProj> projection;
        public TProj current;
    }

    public instance Enumerator_Selection<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, Selection<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
    {
        void Reset(ref Selection<TEnum, TElem, TProj> enumerator) => E.Reset(ref enumerator.source);

        bool MoveNext(ref Selection<TEnum, TElem, TProj> enumerator)
        {
            if (!E.MoveNext(ref enumerator.source))
            {
                return false;
            }

            enumerator.current = enumerator.projection(E.Current(ref enumerator.source));
            return true;
        }

        TProj Current(ref Selection<TEnum, TElem, TProj> enumerator) => enumerator.current;

        void Dispose(ref Selection<TEnum, TElem, TProj> enumerator) { }
    }

    public instance Select_Enumerable<TElem, TProj, [AssociatedType] TSrc, [AssociatedType] TDst, implicit E>
        : CSelect<TElem, TProj, TSrc, Selection<TDst, TElem, TProj>>
        where E : CEnumerable<TSrc, TElem, TDst>
    {
        Selection<TDst, TElem, TProj> Select(TSrc t, Func<TElem, TProj> projection) =>
            new Selection<TDst, TElem, TProj>
            {
                source = E.GetEnumerator(t),
                projection = projection,
                current = default
            };
    }

    /// <summary>
    /// Instance reducing chained Select queries to a single Selection on a
    /// composed projection.
    /// </summary>
    public instance Select_Selection<TElem, TProj1, TProj2, TDest> : CSelect<TProj1, TProj2, Selection<TDest, TElem, TProj1>, Selection<TDest, TElem, TProj2>>
    {
        Selection<TDest, TElem, TProj2> Select(Selection<TDest, TElem, TProj1> t, Func<TProj1, TProj2> projection) =>
            new Selection<TDest, TElem, TProj2>
            {
                source = t.source,
                projection = x => projection(t.projection(x)),
                current = default
            };
    }

    concept CWhere<T, S, [AssociatedType] D>
    {
        D Where(S src, Func<T, bool> f);
    }

    public struct Filtering<TEnum, TElem>
    {
        public TEnum source;
        public Func<TElem, bool> filter;
        public TElem current;
    }

    public instance Enumerator_Filtering<TEnum, [AssociatedType] TElem, implicit E>
        : CEnumerator<TElem, Filtering<TEnum, TElem>>
        where E : CEnumerator<TElem, TEnum>
    {
        void Reset(ref Filtering<TEnum, TElem> enumerator) => E.Reset(ref enumerator.source);

        bool MoveNext(ref Filtering<TEnum, TElem> enumerator)
        {
            do
            {
                if (!E.MoveNext(ref enumerator.source))
                {
                    return false;
                }
                enumerator.current = E.Current(ref enumerator.source);
            } while (!enumerator.filter(enumerator.current));

            return true;
        }

        TElem Current(ref Filtering<TEnum, TElem> enumerator) => enumerator.current;

        void Dispose(ref Filtering<TEnum, TElem> enumerator) => E.Dispose(ref enumerator.source);
    }

    public instance Where_Enumerable<TElem, TSrc, [AssociatedType] TEnum, implicit E>
        : CWhere<TElem, TSrc, Filtering<TEnum, TElem>>
        where E : CEnumerable<TSrc, TElem, TEnum>
    {
        Filtering<TEnum, TElem> Where(TSrc src, Func<TElem, bool> filter) => new Filtering<TEnum, TElem> { source = E.GetEnumerator(src), filter = filter, current = default };
    }

    public struct FilteredSelection<TEnum, TElem, TProj>
    {
        public TEnum source;
        public Func<TElem, TProj> projection;
        public Func<TProj, bool> filter;
        public TProj current;
    }

    public instance Enumerator_FilteredSelection<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, FilteredSelection<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
    {
        void Reset(ref FilteredSelection<TEnum, TElem, TProj> enumerator) => E.Reset(ref enumerator.source);

        bool MoveNext(ref FilteredSelection<TEnum, TElem, TProj> enumerator)
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

        TProj Current(ref FilteredSelection<TEnum, TElem, TProj> enumerator) => enumerator.current;

        void Dispose(ref FilteredSelection<TEnum, TElem, TProj> enumerator) => E.Dispose(ref enumerator.source);
    }

    public instance Where_Selection<TEnum, TElem, TProj> : CWhere<TProj, Selection<TEnum, TElem, TProj>, FilteredSelection<TEnum, TElem, TProj>>
    {
        FilteredSelection<TEnum, TElem, TProj> Where(Selection<TEnum, TElem, TProj> selection, Func<TProj, bool> filter) =>
            new FilteredSelection<TEnum, TElem, TProj>
            {
                source = selection.source,
                projection = selection.projection,
                filter = filter,
                current = default
            };
    }

    public struct SelectedFiltering<TEnum, TElem, TProj>
    {
        public TEnum source;
        public Func<TElem, bool> filter;
        public Func<TElem, TProj> projection;
        public TProj current;
    }

    public instance Enumerator_SelectedFiltering<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, SelectedFiltering<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
    {
        void Reset(ref SelectedFiltering<TEnum, TElem, TProj> enumerator) => E.Reset(ref enumerator.source);

        bool MoveNext(ref SelectedFiltering<TEnum, TElem, TProj> enumerator)
        {
            TElem c = default;
            do
            {
                if (!E.MoveNext(ref enumerator.source))
                {
                    return false;
                }
                c = E.Current(ref enumerator.source);
            } while (!enumerator.filter(c));

            enumerator.current = enumerator.projection(c);
            return true;
        }

        TProj Current(ref SelectedFiltering<TEnum, TElem, TProj> enumerator) => enumerator.current;

        void Dispose(ref SelectedFiltering<TEnum, TElem, TProj> enumerator) => E.Dispose(ref enumerator.source);
    }

    /// <summary>
    /// Instance reducing a Select on a Where to a single composed
    /// SelectedFiltering.
    /// </summary>
    public instance Select_Filtering<TElem, TProj, TDest> : CSelect<TElem, TProj, Filtering<TDest, TElem>, SelectedFiltering<TDest, TElem, TProj>>
    {
        SelectedFiltering<TDest, TElem, TProj> Select(Filtering<TDest, TElem> t, Func<TElem, TProj> projection) =>
            new SelectedFiltering<TDest, TElem, TProj>
            {
                source = t.source,
                filter = t.filter,
                projection = projection,
                current = default
            };
    }

    concept CSelectMany<[AssociatedType] T, [AssociatedType] U, [AssociatedType] V, CT, [AssociatedType] CU, [AssociatedType] CV>
    {
        CV SelectMany(CT src, Func<T, CU> selector, Func<T, U, V> resultSelector);
    }

    instance ListSelectMany<T,U,V> : CSelectMany<T, U, V, List<T>, List<U>, List<V>>
    {
        List<V> SelectMany(List<T> src, Func<T, List<U>> selector, Func<T,U, V> resultSelector)
        {
            var vs = new List<V>();
            foreach (T t in src)
            {
                var us = selector(t);
                foreach (U u in us)
                    vs.Add(resultSelector(t, u));
            }
            return vs;
        }
    }

    instance ArraySelectMany<T, U, V> : CSelectMany<T, U, V, T[],U[],V[]>
    {
        V[] SelectMany(T[] src, Func<T, U[]> selector, Func<T, U, V> resultSelector)
        {
            var vs = new List<V>(); // rather inefficient
            foreach (T t in src)
            {
                var us = selector(t);
                foreach (U u in us)
                    vs.Add(resultSelector(t, u));
            }
            return vs.ToArray();
        }
    }

    /// <summary>
    /// Concept for enumerators whose length is known without enumerating.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the enumerator.
    /// </typeparam>
    public concept CBounded<T>
    {
        /// <summary>
        /// Gets the length of the enumerator.
        /// </summary>
        /// <param name="t">
        /// The enumerator to query.
        /// </param>
        /// <returns>
        /// The length of the enumerator (without enumerating it).
        /// </returns>
        int Bound(ref T t);
    }

    /// <summary>
    /// Instance for O(1) length lookup of array cursors.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance CBounded_ArrayCursor<TElem> : CBounded<Instances.ArrayCursor<TElem>>
    {
        int Bound(ref Instances.ArrayCursor<TElem> t) => t.hi;
    }

    /// <summary>
    /// Instance for O(1) length lookup of selections, when the selected-over
    /// collection is itself bounded.
    /// </summary>
    /// <typeparam name="TEnum">
    /// Type of the source of the selection.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of the elements of <typeparamref name="TEnum"/>.
    /// </typeparam>
    /// <typeparam name="TProj">
    /// Type of the projected elements of the selection.
    /// </typeparam>
    /// <typeparam name="B">
    /// Instance of <see cref="CBounded{T}"/> for <typeparamref name="TEnum"/>.
    /// </typeparam>
    public instance CBounded_Selection<TEnum, TElem, TProj, implicit B> : CBounded<Selection<TEnum, TElem, TProj>>
        where B : CBounded<TEnum>
    {
        int Bound(ref Selection<TEnum, TElem, TProj> sel) => B.Bound(ref sel.source);
    }

    /// <summary>
    /// Concept for types that can be converted to arrays.
    /// </summary>
    /// <typeparam name="TFrom">
    /// Type that is being converted to an array.
    /// </typeparam>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public concept CToArray<TFrom, [AssociatedType] TElem>
    {
        /// <summary>
        /// Converts the argument to an array.
        /// </summary>
        /// <param name="from">
        /// The object from which we are converting.
        /// </param>
        /// <returns>
        /// The array resulting from <paramref name="from"/>.
        /// This may be the same object as <paramref name="from"/>.
        /// </returns>
        TElem[] ToArray(TFrom from);
    }

    /// <summary>
    /// Instance for <see cref="CToArray{TFrom, TElem}"/> when the
    /// source is, itself, an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public instance ToArray_SameArray<TElem> : CToArray<TElem[], TElem>
    {
        TElem[] ToArray(TElem[] from) => from;
    }

    /// <summary>
    /// Baseline instance for <see cref="CToArray{TFrom, TElem}"/>,
    /// when the source is an enumerator.
    /// </summary>
    [Overlappable]
    public instance ToArray_UnboundedEnumerator<TEnum, TElem, implicit E> : CToArray<TEnum, TElem>
        where E : CEnumerator<TElem, TEnum>
    {
        TElem[] ToArray(TEnum e)
        {
            E.Reset(ref e);
            var q = new Queue<TElem>();
            while (E.MoveNext(ref e))
            {
                q.Enqueue(E.Current(ref e));
            }
            return q.ToArray();
        }
    }

    [Overlappable]
    public instance ToArray_BoundedEnumerator<TEnum, TElem, implicit B, implicit E> : CToArray<TEnum, TElem>
        where B : CBounded<TEnum>
        where E : CEnumerator<TElem, TEnum>
    {
        TElem[] ToArray(TEnum e)
        {
            E.Reset(ref e);
            var len = B.Bound(ref e);
            var result = new TElem[len];
            for (var i = 0; i < len; i++)
            {
                E.MoveNext(ref e);
                result[i] = E.Current(ref e);
            }
            return result;
        }
    }
}
