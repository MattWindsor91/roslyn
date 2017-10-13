using System;
using System.Concepts.Prelude;

using ExpressionUtils;
using static ExpressionUtils.Utils;

using BeautifulDifferentiation.ExpInstances;


namespace BeautifulDifferentiation
{

    // Rough perf comparsion between jitted and non-jitted AD
    public class ExpTest
    {
        public static A F<A, implicit FloatA>(A z) where FloatA : Floating<A>
             //=> Sqrt(FromInteger(3) * Sin(z)); // Exp loses
             => (z * z * z * z) + (z * z * z) + (z * z) + z;  // Exp wins

        public static void TestExp<implicit FDA>() where FDA : Floating<D<Exp<double>>>
        {
           
            var d = new D<Exp<double>>(new Var<double>(),new Var<double>());

            var d2 = F(d);

            Console.Out.WriteLine($"D {d.X} {d.DX}");
            Console.Out.WriteLine($"D {d2.X} {d2.DX}");
        }

        const double bound = 100000000.0;
        const double step = 1.0;

        public static void TimeExp<implicit FDA>() where FDA : Floating<D<Exp<double>>>
        {
            var X = new Var<double>();
            var dX = new Var<double>();

            var d = new D<Exp<double>>(X, dX);

            var d2 = F(d);

            var Fx = new Lam<double, double>(X, d2.X);

            Console.WriteLine($"Fx {Fx}");

            var dFx = new Lam<double, Func<double, double>>(dX, new Lam<double, double>(X, d2.DX));

            Console.WriteLine($"dFx {dFx}");

            var cFx = Fx.Compile()();
            var cdFx = dFx.Compile()();

            var sw = new System.Diagnostics.Stopwatch();

            sw.Start();
            var dfOne = cdFx(1.0);
            for (double x = 0.0; x < bound; x = x + step)
            {
                var f = cFx(x);
                var df = dfOne(x);
                if (x < 10.0) System.Console.Write($"{f},{df} ");
            }
            sw.Stop();
            Console.WriteLine($"\nTime: {sw.ElapsedMilliseconds}");;

        }


        public static void TimeDirect<implicit FDA>() where FDA : Floating<D<double>>
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            for (double x = 0.0; x < bound; x = x + step)
            {
                var d  = F(new D<double>(x,1.0));
                if (x < 10.0) System.Console.Write($"{d.X},{d.DX} ");

            }
            sw.Stop();
           
            Console.WriteLine($"\nTime: {sw.ElapsedMilliseconds}");
        }

        public static void Test()
        {
            TestExp<Mark1.FloatingDA<Exp<double>>>();
            TimeExp<Mark1.FloatingDA<Exp<double>>>();
            TimeDirect<Mark1.FloatingDA<double>>();
        }
    }
}
