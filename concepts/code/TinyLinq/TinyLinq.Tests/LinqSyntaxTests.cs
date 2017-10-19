using System;
using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq
{
    static class LinqSyntaxTests
    {
        public static void Run()
        {

            // List queries
            var l = new List<int>(new int[] { 1, 2, 3 });


            var l1 = from x in l where x % 2 == 0 select (double) x;

            var a1 = from x in l from y in l select Tuple.Create(x,y); // needs SelectMany

            // Array queries
            var a = new int[] { 1, 2, 3 };

            var aq = from x in a select x * 5;

            var a2 = from x in a where x % 2 == 0  select (double) x;

            var b = new int[] { 1, 2, 3 };

            var a3 = from x in a from y in b select Tuple.Create(x, y);  // needs SelectMany

            var c = new int[] { 1, 2, 3 };
            var a4 = from x in a where x % 2 == 0  select (double) x;
        }
    }
}
