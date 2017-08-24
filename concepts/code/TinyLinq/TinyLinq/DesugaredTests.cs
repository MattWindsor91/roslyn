using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Showable;

namespace TinyLinq
{   
    static class DesugaredTests
    {
        private static D Select<[AssociatedType]T, [AssociatedType]U, S, [AssociatedType] D, implicit M>(this S This, Func<T, U> f) where M : CSelect<T, U, S, D>

        {

            return M.Select(This, f);

        }

        public static void Run()
        {
            int[] sample = { 1, 0, 0, 9, 7, 3, 2, 5, 3, 3, 7, 6, 5, 2, 0, 1, 3, 5, 8, 6, 3, 4, 6, 7, 3, 5, 4, 8, 7, 6, 8, 0, 9, 5, 9, 0, 9, 1, 1, 7, 3, 9, 2, 9, 2, 7, 4, 9, 4, 5, 3, 7, 5, 4, 2, 0, 4, 8, 0, 5, 6, 4, 8, 9, 4, 7, 4, 2, 9, 6, 2, 4, 8, 0, 5, 2, 4, 0, 3, 7, 2, 0, 6, 3, 6, 1, 0, 4, 0, 2, 0, 0, 8, 2, 2, 9, 1, 6, 6 };

            var f = sample.Select((int x) => x + 5);
            while (CEnumerator<int, Selection<ArrayCursor<int>, int, int>>.MoveNext(ref f))
            {
                Console.WriteLine(CEnumerator<int, Selection<ArrayCursor<int>, int, int>>.Current(ref f));
            }

            Console.WriteLine("oOo");

            var f2 = sample.Select((int x) => x + 5).Select((int y) => y * 10);
            while (CEnumerator<int, Selection<ArrayCursor<int>, int, int>>.MoveNext(ref f2))
            {
                Console.WriteLine(CEnumerator<int, Selection<ArrayCursor<int>, int, int>>.Current(ref f2));
            }

            Console.WriteLine("oOo");

            var g = ((IEnumerable<int>)sample).Select(x => x + 5).GetEnumerator();
            while (g.MoveNext())
            {
                Console.WriteLine(g.Current);
            }
            var g2 = ((IEnumerable<int>)sample).Select(x => x + 5).Select(y => y * 10).GetEnumerator();
            while (g2.MoveNext())
            {
                Console.WriteLine(g2.Current);
            }
        }
    }
}
