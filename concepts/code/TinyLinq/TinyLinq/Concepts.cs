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
    public instance Enumerator_Select<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, Select<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
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
    public instance Select_Enumerator<TElem, TProj, TEnum, implicit E>
        : CSelect<TElem, TProj, TEnum, Select<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
    {
        Select<TEnum, TElem, TProj> Select(TEnum t, Func<TElem, TProj> projection) =>
            new Select<TEnum, TElem, TProj>
            {
                source = t,
                projection = projection,
                current = default
            };
    }

    /// <summary>
    /// Unspecialised instance for selecting over an enumerable, producing
    /// a basic <see cref="Select{TEnum, TElem, TProj}"/>.
    /// </summary>
    [Overlappable]
    public instance Select_Enumerable<TElem, TProj, [AssociatedType] TSrc, [AssociatedType] TDst, implicit E>
        : CSelect<TElem, TProj, TSrc, Select<TDst, TElem, TProj>>
        where E : CEnumerable<TSrc, TElem, TDst>
    {
        Select<TDst, TElem, TProj> Select(TSrc t, Func<TElem, TProj> projection) =>
            new Select<TDst, TElem, TProj>
            {
                source = E.GetEnumerator(t),
                projection = projection,
                current = default
            };
    }

    /// <summary>
    /// Instance reducing chained unspecialised Select queries to a single
    /// <see cref="Select{TEnum, TElem, TProj}"/> on a composed projection.
    /// </summary>
    public instance Select_Select<TElem, TProj1, TProj2, TDest> : CSelect<TProj1, TProj2, Select<TDest, TElem, TProj1>, Select<TDest, TElem, TProj2>>
    {
        Select<TDest, TElem, TProj2> Select(Select<TDest, TElem, TProj1> t, Func<TProj1, TProj2> projection) =>
            new Select<TDest, TElem, TProj2>
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

    /// <summary>
    /// Specialised enumerator for executing Where queries on an array.
    /// </summary>
    /// <typeparam name="TElem">
    /// Type of elements in the array.
    /// </typeparam>
    public struct ArrayWhere<TElem>
    {
        public TElem[] source;
        public Func<TElem, bool> filter;
        public int lo;
        public int hi;
    }

    /// <summary>
    /// Enumerator instance for filtered arrays.
    /// </summary>
    public instance Enumerator_ArrayWhere<TElem> : CEnumerator<TElem, ArrayWhere<TElem>>
    {
        void Reset(ref ArrayWhere<TElem> enumerator)
        {
            enumerator.lo = -1;
        }

        bool MoveNext(ref ArrayWhere<TElem> enumerator)
        {
            if (enumerator.hi <= enumerator.lo)
            {
                return false;
            }

            enumerator.lo++;
            while (enumerator.lo < enumerator.hi)
            {
                if (enumerator.filter(enumerator.source[enumerator.lo]))
                {
                    return true;
                }
                enumerator.lo++;
            }

            return false;
        }

        TElem Current(ref ArrayWhere<TElem> enumerator)
        {
            if (enumerator.lo == -1)
            {
                return default;
            }
            return enumerator.source[enumerator.lo];
        }

        void Dispose(ref ArrayWhere<TElem> enumerator) { }
    }

    /// <summary>
    /// Specialised instance for executing Where queries on an array.
    /// </summary>
    public instance Where_Array<TElem> : CWhere<TElem, TElem[], ArrayWhere<TElem>>
    {
        ArrayWhere<TElem> Where(TElem[] src, Func<TElem, bool> f) =>
            new ArrayWhere<TElem> { source = src, filter = f, lo = -1, hi = src.Length };
    }

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
        public TEnum source;
        public Func<TElem, TProj> projection;
        public Func<TProj, bool> filter;
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for fused Wheres on unspecialised Selects.
    /// </summary>
    public instance Enumerator_WhereSelect<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, WhereOfSelect<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
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
    public instance Where_Select<TEnum, TElem, TProj> : CWhere<TProj, Select<TEnum, TElem, TProj>, WhereOfSelect<TEnum, TElem, TProj>>
    {
        WhereOfSelect<TEnum, TElem, TProj> Where(Select<TEnum, TElem, TProj> selection, Func<TProj, bool> filter) =>
            new WhereOfSelect<TEnum, TElem, TProj>
            {
                source = selection.source,
                projection = selection.projection,
                filter = filter,
                current = default
            };
    }

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
        public TEnum source;
        public Func<TElem, bool> filter;
        public Func<TElem, TProj> projection;
        public TProj current;
    }

    public instance Enumerator_SelectWhere<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, SelectOfWhere<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
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
        SelectOfWhere<TDest, TElem, TProj> Select(Where<TDest, TElem> t, Func<TElem, TProj> projection) =>
            new SelectOfWhere<TDest, TElem, TProj>
            {
                source = t.source,
                filter = t.filter,
                projection = projection,
                current = default
            };
    }

    public struct ArraySelectOfWhere<TElem, TProj>
    {
        public TElem[] source;
        public Func<TElem, bool> filter;
        public Func<TElem, TProj> projection;
        public int lo;
        public int hi;
        public TProj current;
    }

    /// <summary>
    /// Enumerator instance for selections of filtered arrays.
    /// </summary>
    public instance Enumerator_ArraySelectOfWhere<TElem, TProj> : CEnumerator<TProj, ArraySelectOfWhere<TElem, TProj>>
    {
        void Reset(ref ArraySelectOfWhere<TElem, TProj> sw)
        {
            sw.lo = -1;
            sw.current = default;
        }

        bool MoveNext(ref ArraySelectOfWhere<TElem, TProj> sw)
        {
            if (sw.hi <= sw.lo)
            {
                return false;
            }

            sw.lo++;
            while (sw.lo < sw.hi)
            {
                if (sw.filter(sw.source[sw.lo]))
                {
                    sw.current = sw.projection(sw.source[sw.lo]);
                    return true;
                }
                sw.lo++;
            }

            return false;
        }

        TProj Current(ref ArraySelectOfWhere<TElem, TProj> sw) => sw.current;

        void Dispose(ref ArraySelectOfWhere<TElem, TProj> enumerator) { }
    }

    /// <summary>
    /// Instance reducing a Select on a filtered array cursor to a single
    /// composed <see cref="SelectedFilteredArrayCursor{TElem, TProj}"/>.
    /// </summary>
    public instance Select_Where_Array<TElem, TProj> : CSelect<TElem, TProj, ArrayWhere<TElem>, ArraySelectOfWhere<TElem, TProj>>
    {
        ArraySelectOfWhere<TElem, TProj> Select(ArrayWhere<TElem> t, Func<TElem, TProj> projection) =>
            new ArraySelectOfWhere<TElem, TProj>
            {
                source = t.source,
                filter = t.filter,
                projection = projection,
                lo = -1,
                hi = t.hi,
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
    public instance CBounded_Select<TEnum, TElem, TProj, implicit B> : CBounded<Select<TEnum, TElem, TProj>>
        where B : CBounded<TEnum>
    {
        int Bound(ref Select<TEnum, TElem, TProj> sel) => B.Bound(ref sel.source);
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
