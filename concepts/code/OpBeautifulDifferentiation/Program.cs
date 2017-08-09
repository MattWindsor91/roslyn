using System;
using System.Concepts.OpPrelude;
using static System.Concepts.OpPrelude.Verbose;

/// <summary>
///     Implementation of parts of Conal Elliot's Beautiful Differentiation.
///     This version uses the experimental operator-overloaded prelude.
/// </summary>
namespace OpBeautifulDifferentiation
{
    public class Program
    {
        public static A F<A, implicit FloatA>(A z) where FloatA : Floating<A>
            => Sqrt(Mul(FromInteger(3), Sin(z)));

        public static A G<A, implicit FloatA>(A z) where FloatA : Floating<A>
            => Mul(Mul(FromInteger(3), Asinh(z)), Log(z));

        public static void Test<implicit FDA>() where FDA : Floating<D<double>>
        {
            var d = new D<double>(2.0, 1.0);

            var d2 = F(d);
            var d3 = G(d);

            Console.Out.WriteLine($"D {d.X} {d.DX}");
            Console.Out.WriteLine($"D {d2.X} {d2.DX}");
            Console.Out.WriteLine($"D {d3.X} {d3.DX}");
        }

        public static void Main()
        {
            Test<Mark1.FloatingDA<double>>();
            Test<Mark2.FloatingDA<double>>();
        }
    }
}