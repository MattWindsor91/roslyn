// This tests that we're selecting the correct witness out of a pair
// of mildly ambiguous choices.
using System;
using System.Concepts.Prelude;

public class Fail
{
    public bool FailEq<A>(A a, A b)
        where EqA1 : Eq<A>
        where EqA2 : Eq<int>
    {
        return Equals(a, b);
    }

    public bool FailEq2(int a, int b)
        where EqA1 : Eq<bool>
        where EqA2 : Eq<int>
    {
        return Equals(a, b);
    }

    public bool FailEq3(int a, int b)
        where EqA1 : Eq<int>
        where EqA2 : Eq<bool>
    {
        return Equals(a, b);
    }

    public static void Main()
    {
        var f = new Fail();
        Console.Out.WriteLine(f.FailEq<int, OrdInt, OrdInt>(1, 2) ? "fail" : "pass");
        Console.Out.WriteLine(f.FailEq2<OrdBool, OrdInt>(1, 2) ? "fail" : "pass");
        Console.Out.WriteLine(f.FailEq3<OrdInt, OrdBool>(1, 2) ? "fail" : "pass");
    }
}