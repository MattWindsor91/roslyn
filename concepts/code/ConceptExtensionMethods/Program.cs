using System.Collections.Generic;
using System.Concepts;
using System.Linq;

namespace ConceptExtensionMethods
{
    public concept CIntExtensions
    {
        int Minus(this int x, int y);
    }
    public instance IntExtensions : CIntExtensions
    {
        int Minus(this int x, int y) => x - y;
    }

    public concept CMonoid<T>
    {
        T Plus(this T me, T you);

        T Zero { get; }
    }

    public instance Monoid_Int : CMonoid<int>
    {
        int Plus(this int me, int you) => me + you;

        int Zero => 0;
    }

    public instance Monoid_List<T> : CMonoid<List<T>>
    {
        List<T> Plus(this List<T> me, List<T> you) => new List<T>(me.Concat(you));

        List<T> Zero => new List<T>();
    }

    static class Extensions
    {
        public static A EPlus<A, implicit MA>(this A x, A y) where MA : CMonoid<A>
        {
            var dead = Plus(x, y);

            return x.Plus(y);
        }
    }

    public concept Num<A>
    {
        A Add(this A x, A y);
        A Sub(this A x, A y) => x.Add(y.Neg());
        A Neg(this A x) => FromInteger(0).Sub(x);
        A FromInteger(int x);
    }

    class Program
    {
        private static A Wrap<A, implicit MA>(A x, A y) where MA : CMonoid<A>
        {
            return x.Plus(y);
        }

        static void Main(string[] args)
        {
            // Static invocation
            System.Console.Write("Static:");
            foreach (var i in CMonoid<List<int>>.Plus(new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 }))
            {
                System.Console.Write(" ");
                System.Console.Write(i);
            }
            System.Console.WriteLine();

            // Wrapped extension invocation
            System.Console.Write("CEM(W):");
            foreach (var i in Wrap(new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 }))
            {
                System.Console.Write(" ");
                System.Console.Write(i);
            }
            System.Console.WriteLine();

            // Nested extension invocation
            System.Console.Write("CEM(N):");
            foreach (var i in (new List<int> { 1, 2, 3 }).EPlus(new List<int> { 4, 5, 6 }))
            {
                System.Console.Write(" ");
                System.Console.Write(i);
            }
            System.Console.WriteLine();

            // Extension invocation
            System.Console.Write("CEM:   ");
            foreach (var i in (new List<int> { 1, 2, 3 }).Plus(new List<int> { 4, 5, 6 }))
            {
                System.Console.Write(" ");
                System.Console.Write(i);
            }
            System.Console.WriteLine();

            System.Console.WriteLine(1.Minus(3));
            System.Console.WriteLine(CMonoid<int>.Zero);
        }
    }
}
