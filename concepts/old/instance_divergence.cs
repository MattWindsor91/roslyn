// Test to make sure type inference does not fail to terminate on
// diverging instance derivations.

concept Foo<A>
{
}

concept Bar<A>
{
}

concept Baz<A>
{
}

instance FooBar<A> : Foo<A> where BarA : Bar<A[]>
{
}

instance BarFoo<A> : Bar<A> where FooA : Foo<A[]>
{
}

instance BazBaz<A> : Baz<A> where BazA : Baz<A[]>
{
}

static class Program
{
    // If there is no checking for divergence, this will stack overflow.
    public static void Crash<A>(A a) where FooA : Foo<A>
    {
    }

    // As should this.
    public static void Crash2<A>(A a) where FooA : Foo<A>
    {
    }

    public static void Main()
    {
        Crash(3);
        Crash2(true);
    }
}