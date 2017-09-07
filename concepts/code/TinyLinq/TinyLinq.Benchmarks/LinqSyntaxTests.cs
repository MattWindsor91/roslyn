using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq
{
    static class LinqSyntaxTests
    {
        private static TDst Select<TSrc, [AssociatedType]TDst, [AssociatedType]TElem, [AssociatedType]TProj, implicit M>(this TSrc This, Func<TElem, TProj> f) where M : CSelect<TElem, TProj, TSrc, TDst> =>
    M.Select(This, f);

        private static TDest Where<TSrc, [AssociatedType]TElem, [AssociatedType]TDest, implicit M>(this TSrc This, Func<TElem, bool> f) where M : CWhere<TSrc, TElem, TDest> =>
    M.Where(This, f);

        private static SelectMany<List<T>, T, List<U>, List<U>.Enumerator, U, V> SelectMany<[AssociatedType]T, [AssociatedType]U, [AssociatedType]V, implicit M>(this List<T> This, Func<T, List<U>> selector, Func<T, U, V> resultSelector)
            where M : CSelectMany<List<T>, T, List<U>, U, V, SelectMany<List<T>, T, List<U>, List<U>.Enumerator, U, V>>
        {
            return M.SelectMany(This, selector, resultSelector);
        }

        public static void Run()
        {
            // List queries
            List<int> l = new List<int>(new int[] { 1, 2, 3 });


            //var l1 = from x in l where x % 2 == 0 select (double) x;

            //List<Tuple<int,int>> a1 = from x in l from y in l select Tuple.Create(x,y); // needs SelectMany

            // Array queries
            int[] a = new int[] { 1, 2, 3 };

            var ac = CEnumerable<int[]>.GetEnumerator(a);
            var aq = from x in a select x * 5;

            //Selection<ArrayCursor<int>, int, double> a2 = from x in a where x % 2 == 0  select (double) x;

            int[] b = new int[] { 1, 2, 3 };

            //Tuple<int, int>[] a3 = from x in a from y in b select Tuple.Create(x, y);  // needs SelectMany

            int[] c = new int[] { 1, 2, 3 };
            //Selection<ArrayCursor<int>, int, double> a4 = from x in a where x % 2 == 0  select (double) x;
        }
    }
}
