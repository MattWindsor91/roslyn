using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Enumerable;

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
            int[] sample = { 2, 4, 5, 10, 99, 398, 34 };

            var f = sample.Select((int x) => x + 5);
            while (CEnumerator<int, Selection<ArrayCursor<int>, int, int>>.MoveNext(ref f))
            {
                Console.WriteLine(CEnumerator<int, Selection<ArrayCursor<int>, int, int>>.Current(ref f));
            }

            Console.WriteLine("oOo");

            var g = ((IEnumerable<int>)sample).Select((x) => x + 5).GetEnumerator();
            while (g.MoveNext())
            {
                Console.WriteLine(g.Current);
            }
        }
    }
}
