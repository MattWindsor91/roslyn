using System;
using System.Concepts.OpPrelude;
using System.Concepts.OpNumerics;
using System.Numerics;

namespace OpsTestbed
{
    class Program
    {
        static A M<A, implicit NumA>(A x) where NumA : Num<A> => FromInteger(666) * x * x * x + FromInteger(777) * x * x + FromInteger(888);
        static A N<A, implicit NumA>(A x) where NumA : Num<A>
        {
            var y = FromInteger(0);
            for (int i = 0; i < 100; ++i)
                y = FromInteger(666) * x * x * x + FromInteger(777) * x * x + FromInteger(888);
            return y;
        }

        static void Main(string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            System.Diagnostics.Debugger.Break();

            Console.WriteLine(M(16)); // int
            Console.WriteLine(M(16.0)); // double
            Console.WriteLine(M(new Vector2(16, 8)));

            Console.WriteLine(N(16)); // int
            Console.WriteLine(N(16.0)); // double
            Console.WriteLine(N(new Vector2(16, 8)));

            var v = new Vector2(16, 8);
            var m = new Vector2(666) * v * v * v + new Vector2(777) * v * v + new Vector2(888);
            Console.WriteLine(m);
        }
    }
}

