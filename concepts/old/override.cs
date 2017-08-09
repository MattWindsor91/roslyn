// Tests to see if overriding of concept-constrained methods works.

using System;

public class Foo
{
    concept Show<A>
    {
        string Show(A toShow);
    }

    instance ShowInt : Show<int>
    {
        string Show(int toShow) => toShow.ToString();
    }

    class Printer
    {
        public virtual void Print<A>(A toPrint) where ShowA: Show<A>
        {
            Console.Out.WriteLine(ShowA.Show(toPrint));
        }
    }

    class FancyPrinter : Printer
    {
        public override void Print<B>(B toPrint) where ShowB: concept
        {
            Console.Out.WriteLine("oOo");
            Console.Out.WriteLine(ShowB.Show(toPrint));
            Console.Out.WriteLine("oOo");
        }
    }

    public static void Main()
    {
        new Printer().Print<int, ShowInt>(27);
        new FancyPrinter().Print<int, ShowInt>(53);
    }
}