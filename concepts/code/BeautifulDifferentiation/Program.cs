using System;
using System.Concepts.Prelude;

/// <summary>
///     Implementation of parts of Conal Elliot's Beautiful Differentiation.
///     This version uses the experimental operator-overloaded prelude.
/// </summary>
namespace BeautifulDifferentiation
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

        public static void TestHigherOrder<implicit FHDA>() where FHDA : Floating<HoD<double>>
        {
            var d = HoD<double>.Id(2.0);

            var d2 = F(d);
            var d3 = G(d);

            Console.Out.WriteLine($"HD {d.X} {d.DX.Value.X} {d.DX.Value.DX.Value.X}");
            Console.Out.WriteLine($"HD {d2.X} {d2.DX.Value.X} {d2.DX.Value.DX.Value.X}");
            Console.Out.WriteLine($"HD {d3.X} {d3.DX.Value.X} {d3.DX.Value.DX.Value.X}");
        }

        public static void Main()
        {
            Test<Mark1.FloatingDA<double>>();
            Test<Mark2.FloatingDA<double>>();
            TestHigherOrder<HoMark2.FloatingDA<double>>();
        }
    }
}