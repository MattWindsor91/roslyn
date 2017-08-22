using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Concepts;

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

    concept CSelect<[AssociatedType] T, [AssociatedType] U, S, [AssociatedType] D>
    {
        D Select(S src, Func<T, U> f);
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

    instance ArraySelect<T, U> : CSelect<T, U, T[], U[]>
    {
        U[] Select(T[] src, Func<T, U> f)
        {
            var l = new U[src.Length];
            for (int i = 0; i < src.Length; i++)
                l[i] = f(src[i]);
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


    static class ListExtensions
    {

        public static D Select<[AssociatedType]T, [AssociatedType]U, [AssociatedType] D, implicit M>(this List<T> This, Func<T, U> f) where M : CSelect<T, U, List<T>, D>
        {
            return M.Select(This, f);
        }
        
        public static List<T> Where<[AssociatedType]T, implicit M>(this List<T> This, Func<T, bool> f) where M : CWhere<T, List<T>>
        {
            return M.Where(This, f);
        }

        public static List<V> SelectMany<[AssociatedType]T, [AssociatedType]U, [AssociatedType]V, implicit M>(this List<T> This, Func<T, List<U>> selector, Func<T,U,V> resultSelector) 
            where M : CSelectMany<T, U, V, List<T>, List<U>, List<V>>
        {
            return M.SelectMany(This, selector, resultSelector);
        }

    
    }


    static class ArrayExtensions
    {

        public static D Select<[AssociatedType]T, [AssociatedType]U, [AssociatedType] D, implicit M>(this T[] This, Func<T, U> f) where M : CSelect<T, U, T[], D>
        {
            return M.Select(This, f);
        }

        public static T[] Where<[AssociatedType]T, implicit M>(this T[] This, Func<T, bool> f) where M : CWhere<T, T[]>
        {
            return M.Where(This, f);
        }

        public static V[] SelectMany<[AssociatedType]T, [AssociatedType]U, [AssociatedType]V, implicit M>(this T[] This, Func<T, U[]> selector, Func<T, U, V> resultSelector)
            where M : CSelectMany<T, U, V, T[], U[], V[]>
        {
            return M.SelectMany(This, selector, resultSelector);
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            // List queries
            List<int> l = new List<int>(new int[] { 1, 2, 3 });

            List<double> l1 = from x in l where x % 2 == 0 select (double) x;

            List<Tuple<int,int>> a1 = from x in l from y in l select Tuple.Create(x,y); // needs SelectMany

            // Array queries
            int[] a = new int[] { 1, 2, 3 };
            double[] a2 = from x in a where x % 2 == 0  select (double) x;

            int[] b = new int[] { 1, 2, 3 };

            Tuple<int, int>[] a3 = from x in a from y in b select Tuple.Create(x, y);  // needs SelectMany

        }
    }
}
   
