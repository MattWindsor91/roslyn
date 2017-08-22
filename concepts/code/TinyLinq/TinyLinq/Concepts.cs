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
    concept CSelect<[AssociatedType] T, [AssociatedType] U, S, D>
    {
        D Select(S src, Func<T, U> f);
    }

    public struct ArrayCursor<TElem>
    {
        public TElem[] source;
        public int lo;
        public int hi;
    }

    public instance Enumerator_ArrayCursor<TElem> : CEnumerator<TElem, ArrayCursor<TElem>>
    {
        void Reset(ref ArrayCursor<TElem> enumerator)
        {
            enumerator.lo = -1;
        }

        bool MoveNext(ref ArrayCursor<TElem> enumerator)
        {
            // hi always points to one index beyond the end of the array slice
            if (enumerator.hi <= enumerator.lo + 1)
            {
                return false;
            }
            enumerator.lo++;
            return true;
        }

        TElem Current(ref ArrayCursor<TElem> enumerator)
        {
            if (enumerator.lo == -1)
            {
                return default;
            }
            return enumerator.source[enumerator.lo];
        }

        void Dispose(ref ArrayCursor<TElem> enumerator) { }
    }

    public struct Selection<TEnum, TElem, TProj>
    {
        public TEnum source;
        public Func<TElem, TProj> projection;
    }

    public instance Enumerator_Selection<TEnum, [AssociatedType] TElem, TProj, implicit E>
        : CEnumerator<TProj, Selection<TEnum, TElem, TProj>>
        where E : CEnumerator<TElem, TEnum>
    {
        void Reset(ref Selection<TEnum, TElem, TProj> enumerator)
        {
            E.Reset(ref enumerator.source);
        }

        bool MoveNext(ref Selection<TEnum, TElem, TProj> enumerator)
        {
            if (!E.MoveNext(ref enumerator.source))
            {
                return false;
            }
            return true;
        }

        TProj Current(ref Selection<TEnum, TElem, TProj> enumerator)
        {
            return enumerator.projection(E.Current(ref enumerator.source));
        }

        void Dispose(ref Selection<TEnum, TElem, TProj> enumerator) { }
    }


    public instance Select_Array<TElem, TProj> : CSelect<TElem, TProj, TElem[], Selection<ArrayCursor<TElem>, TElem, TProj>>
    {
        Selection<ArrayCursor<TElem>, TElem, TProj> Select(TElem[] t, Func<TElem, TProj> projection)
        {
            return new Selection<ArrayCursor<TElem>, TElem, TProj>
            {
                source = new ArrayCursor<TElem> { source = t, lo = -1, hi = t.Length },
                projection = projection
            };
        }
    }

    instance ListSelect<T, U> : CSelect<T, U, List<T>, List<U>>
    {
        List<U> Select(List<T> src, Func<T, U> f)
        {
            var l = new List<U>(src.Capacity);
            foreach (var e in src)
                l.Add(f(e));
            return l;
        }
    }

    concept CWhere<[AssociatedType] T, S>
    {
        S Where(S src, Func<T, bool> f);
    }

    instance ListWhere<T> : CWhere<T, List<T>>
    {
        List<T> Where(List<T> src, Func<T, bool> f)
        {
            var l = new List<T>(src.Capacity);
            foreach (var e in src)
                if (f(e)) l.Add(e);
            return l;
        }
    }

    instance ArrayWhere<T> : CWhere<T, T[]>
    {
        T[] Where(T[] src, Func<T, bool> f)
        {
            var l = new List<T>(src.Length); // rather inefficient
            foreach (var e in src)
                if (f(e)) l.Add(e);
            return l.ToArray();
        }
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
}
