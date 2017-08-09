using System;
using System.Collections.Generic;


/// <summary>
/// This corresponds roughly to figure 1 of the paper, and is separate from
/// the other examples so we can use 'using static' in the driver without
/// polluting A and B with each other's monoids.
/// </summary>
namespace MonoidExamples
{
    using static Monoid;

    concept Monoid<A>
    {
        A BinaryOp(A x, A y);
        A Identity();
    }

    // Scala:
    //   trait Monoid[A] {
    //     def binary_op (x: A, y: A) : A
    //     def identity               : A
    //   }

    static class Monoid
    {
        public static A Acc<A, implicit M>(List<A> l) where M : Monoid<A>
        {
            // We don't have left folds!
            A result = M.Identity();
            foreach (A a in l)
            {
                result = M.BinaryOp(result, a);
            }
            return result;
        }

        // Scala:
        //   def acc[A] (l: List[A]) (implicit m: Monoid[A]) : A =
        //     l.foldLeft (m.identity) ((x, y) => m.binary_op (x, y))
    }

    static class A
    {
        public instance SumMonoid : Monoid<int>
        {
            int BinaryOp(int x, int y) => x + y;
            int Identity() => 0;
        }

        public static int Sum(List<int> l) => Acc(l);
    }

    // Scala:
    //   object A {
    //     implicit object SumMonoid extends Monoid[Int] {
    //       def binary_op (x: Int, y: Int) = x + y
    //       def Identity() => 0
    //     }
    //     def sum (l: List[Int]) : Int = acc(l)
    //   }

    static class B
    {
        public instance ProdMonoid : Monoid<int>
        {
            int BinaryOp(int x, int y) => x * y;
            int Identity() => 1;
        }

        public static int Product(List<int> l) => Acc(l);
    }

    // Scala:
    //   object B {
    //     implicit object ProdMonoid extends Monoid[Int] {
    //       def binary_op (x: Int, y: Int) = x * y
    //       def Identity() => 1
    //     }
    //     def product (l: List[Int]) : Int = acc(l)
    //   }
}

/// <summary>
/// C# encoding of examples from section 3 of d. S. Oliviera et al.
/// </summary>
namespace TCOIExamples
{
    // For Acc.
    using static MonoidExamples.Monoid;

    // This is as close as we get to Scala's import keyword.
    using static MonoidExamples.A;
    using static MonoidExamples.B;

    static class Section3
    {
        public static void Examples()
        {
            // Figure 1: Locally scoped implicits.
            //
            // This is less compelling than Scala because we don't have Scala's
            // import method.  We have 'using static', but we have to do
            // namespace-fu to allow the monoid instances to be public while not
            // importing each other by accident.

            Console.Out.WriteLine("> l = List(1, 2, 3, 4, 5)");
            var l = new List<int> { 1, 2, 3, 4, 5 };
            Console.Out.WriteLine($"> Sum(l) = {Sum(l)}");
            Console.Out.WriteLine($"> Product(l) = {Product(l)}");
            Console.Out.WriteLine($"> Acc(ProdMonoid) (l) = {Acc<int, ProdMonoid>(l)}");
        }
    }
}