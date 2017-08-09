using System;
using System.Collections.Generic;

/// <summary>
/// C# encoding of examples from section 1 of d. S. Oliviera et al.
/// </summary>
namespace TCOIExamples
{
    static class Section1
    {
        public static List<T> Sort<T, implicit OrdT>(List<T> xs) where OrdT : Ord<T>
        {
            // Unlike the paper, we give an implementation of Sort.

            T[] a = xs.ToArray();
            Qsort(a, 0, a.Length - 1);
            return new List<T>(a);
        }

        // Scala:
        //   def sort[T] (xs: List[T]) (implicit ordT : Ord[T]) : List[T]
        //
        // The main difference here is that we're passing a type, rather than
        // an object.

        #region Implementation details not in the paper

        private static void Qsort<T, implicit OrdT>(T[] xs, int lo, int hi) where OrdT : Ord<T>
        {
            if (lo < hi)
            {
                var p = Partition(xs, lo, hi);
                Qsort(xs, lo, p - 1);
                Qsort(xs, p + 1, hi);
            }
        }

        private static int Partition<T, implicit OrdT>(T[] xs, int lo, int hi) where OrdT : Ord<T>
        {
            var pivot = xs[hi];
            var i = lo;
            for (int j = lo; j < hi; j++)
            {
                if (Compare(xs[j], pivot))
                {
                    var tmp1 = xs[i];
                    xs[i] = xs[j];
                    xs[j] = tmp1;
                    i++;
                }
            }

            var tmp2 = xs[i];
            xs[i] = xs[hi];
            xs[hi] = tmp2;

            return i;
        }

        #endregion Implementation details not in the paper

        // Ord is defined in 'Ord.cs'.

        public instance IntOrd : Ord<int>
        {
            bool Compare(int a, int b) => a <= b;
        }

        // Scala:
        //   implicit object intOrd extends Ord[Int] {
        //     def compare (a: Int, b: Int) : Boolean = a <= b
        //   }

        public static void Examples()
        {
            Console.Out.Write("> sort(List(3, 2, 1) = ");
            PrintList(Sort(new List<int> { 3, 2, 1 }));
        }

        private static void PrintList<A>(List<A> args)
        {
            Console.Out.Write("List(");
            if (0 < args.Count)
            {
                Console.Out.Write(args[0].ToString());
                for (int i = 1; i < args.Count; i++)
                {
                    Console.Out.Write(", ");
                    Console.Out.Write(args[i].ToString());
                }
            }
            Console.Out.WriteLine(")");
        }
    }
}