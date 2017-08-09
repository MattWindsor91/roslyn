// Examples using System.Concepts.Monoid.

using System.Concepts.Prelude;
using System.Concepts.Monoid;
using System.Text;
using System;
using static System.Concepts.Monoid.Utils;
using static ArrayHelp;

static class ArrayHelp
{
    public static string ShowArray<A>(A[] xs) {
        var sb = new StringBuilder("[");
        var l = xs.Length;
        for (int i = 0; i < l; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(xs[i].ToString());
        }

        sb.Append("]");
        return sb.ToString();
    }
}

class NumMonoidTest<A>
{
    private A[] _xs;

    public NumMonoidTest(A[] xs)
    {
        _xs = xs;
    }

    public void Run() where NumA : Num<A>
    {
        var xss = ShowArray(_xs);

        var sum = Concat<A, Sum<A, NumA>>(_xs);
        Console.Out.WriteLine($"Sum {xss} = {sum}");

        var product = Concat<A, Product<A, NumA>>(_xs);
        Console.Out.WriteLine($"Product {xss} = {product}");
    }
}

class OrdSemiTest<A>
{
    private A[] _xs;

    public OrdSemiTest(A[] xs)
    {
        _xs = xs;
    }


    public void Run() where OrdA : Ord<A>
    {
        var xss = ShowArray(_xs);

        var min = ConcatNonEmpty<A, Min<A, OrdA>>(_xs);
        Console.Out.WriteLine($"Min {xss} = {min}");

        var max = ConcatNonEmpty<A, Max<A, OrdA>>(_xs);
        Console.Out.WriteLine($"Max {xss} = {max}");
    }
}

static class MonoidExamples
{
    static void RunNumOrd<A>(A[] xs)
        where NumA : Num<A>
        where OrdA : Ord<A>
    {
        new NumMonoidTest<A>(xs).Run();
        new OrdSemiTest<A>(xs).Run();
    }

    public static void Main()
    {
        new NumMonoidTest<int>(new int[] {}).Run();
        RunNumOrd(new int[] {6, 3, 1, 2, 10, 121});
        RunNumOrd(new double[] {6.2, 3.3, 1.1, 2.4, 10.5, 121.6});

        var bools = new bool[] { true, true, false, true, false };
        var boolss = ShowArray(bools);
        var any = Concat<bool, Any>(bools);
        Console.Out.WriteLine($"Any {boolss} = {any}");
        var all = Concat<bool, All>(bools);
        Console.Out.WriteLine($"All {boolss} = {all}");
    }
}