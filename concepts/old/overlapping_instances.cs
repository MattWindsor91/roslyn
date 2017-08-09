// Checks tie-breaking works for overlapping instance examples.
using System;
using System.Concepts.Prelude;

public struct Foo
{
    // Intentionally left blank.
}

//
// OVERLAPS BY CONCEPT INHERITANCE
//

// Now, give deliberately contradictory instances for Eq and Ord, so we
// can tell which one is picked up...

public instance EqFoo : Eq<Foo>
{
    bool Equals(Foo a, Foo b) => true;
}

public instance OrdFoo : Ord<Foo>
{
    bool Equals(Foo a, Foo b) => false;
    bool Leq(Foo a, Foo b) => true;
}

//
// OVERLAPS BY TYPE PARAMETER SUBSUMPTION
//

public concept Noot<A>
{
    bool Noot(A a);
}

// Now, give deliberately contradictory instances for Noot<A> and
// Noot<Bar>.

public instance NootA<A> : Noot<A>
{
    bool Noot(A a) => true;
}

public instance NootFoo : Noot<Foo>
{
    bool Noot(Foo foo) => false;
}

//
// DRIVER
//

public static class Program
{
    public static bool Equals<A>(A a, A b) where EqA : Eq<A>
    {
        return Equals(a, b);
    }

    public static bool Noot<A>(A a) where NootA : Noot<A>
    {
        return Noot(a);
    }

    public static void Main()
    {
        // This should choose OrdFoo here.
        Console.Out.WriteLine(
            Equals(default(Foo), default(Foo))
            ? "Fail: chose EqFoo"
            : "Pass: chose OrdFoo"
        );

        // This should choose NootBar here.
        Console.Out.WriteLine(
            Noot(default(Foo))
            ? "Fail: chose NootA"
            : "Pass: chose NootFoo"
        );
    }
}