using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    static class LinqSyntaxTests
    {
        // List extensions

        private static D Select<[AssociatedType]T, [AssociatedType]U, [AssociatedType] D, implicit M>(this List<T> This, Func<T, U> f) where M : CSelect<T, U, List<T>, D>
        {
            return M.Select(This, f);
        }

        private static List<T> Where<[AssociatedType]T, implicit M>(this List<T> This, Func<T, bool> f) where M : CWhere<T, List<T>>
        {
            return M.Where(This, f);
        }

        private static List<V> SelectMany<[AssociatedType]T, [AssociatedType]U, [AssociatedType]V, implicit M>(this List<T> This, Func<T, List<U>> selector, Func<T, U, V> resultSelector)
            where M : CSelectMany<T, U, V, List<T>, List<U>, List<V>>
        {
            return M.SelectMany(This, selector, resultSelector);
        }

        // Array extensions

        private static D Select<[AssociatedType]T, [AssociatedType]U, [AssociatedType] D, implicit M>(this T[] This, Func<T, U> f) where M : CSelect<T, U, T[], D>
        {
            return M.Select(This, f);
        }

        private static T[] Where<[AssociatedType]T, implicit M>(this T[] This, Func<T, bool> f) where M : CWhere<T, T[]>
        {
            return M.Where(This, f);
        }

        private static V[] SelectMany<[AssociatedType]T, [AssociatedType]U, [AssociatedType]V, implicit M>(this T[] This, Func<T, U[]> selector, Func<T, U, V> resultSelector)
            where M : CSelectMany<T, U, V, T[], U[], V[]>
        {
            return M.SelectMany(This, selector, resultSelector);
        }

        public static void Run()
        {
            // List queries
            List<int> l = new List<int>(new int[] { 1, 2, 3 });

            List<double> l1 = from x in l where x % 2 == 0 select (double) x;

            List<Tuple<int,int>> a1 = from x in l from y in l select Tuple.Create(x,y); // needs SelectMany

            // Array queries
            int[] a = new int[] { 1, 2, 3 };
            Selection<ArrayCursor<int>, int, double> a2 = from x in a where x % 2 == 0  select (double) x;

            int[] b = new int[] { 1, 2, 3 };

            Tuple<int, int>[] a3 = from x in a from y in b select Tuple.Create(x, y);  // needs SelectMany

            int[] c = new int[] { 1, 2, 3 };
            Selection<ArrayCursor<int>, int, double> a4 = from x in a where x % 2 == 0  select (double) x;

        }
    }
}
