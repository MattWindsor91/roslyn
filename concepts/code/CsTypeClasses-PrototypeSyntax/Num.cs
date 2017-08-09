// overloading numeric operations
namespace Num
{
    using System;
    using Eq;
    concept Num<A>
    {
        A Add(A a, A b);
        A Mult(A a, A b);
        A Neg(A a);
    }

    static class Overloads
    {
        public static A Add<A, implicit NumA>(A a, A b) where NumA : Num<A> => Add(a, a);

        public static A Mult<A, implicit NumA>(A a, A b) where NumA : Num<A> => Mult(a, a);

        public static A Neg<A, implicit NumA>(A a) where NumA : Num<A> => Neg(a);
    }

    instance NumInt : Num<int>
    {
        int Add(int a, int b) => a + b;
        int Mult(int a, int b) => a * b;
        int Neg(int a) => -a;
    }

    class Test
    {

        static A Square<A, implicit NumA>(A a) where NumA : Num<A>
        {
            return Overloads.Mult(a, a);
        }

        static bool MemSq<A, implicit NumA, implicit EqA>(A[] a_s, A a)
          where NumA : Num<A>
          where EqA : Eq<A>
        {
            for (int i = 0; i < a_s.Length; i++)
            {
                if (Eq.Overloads.Eq(a_s[i], Square(a)))
                {
                    return true;
                }
            }
            return false;
        }


        public static void Run()
        {

            Console.WriteLine("NumTest {0}",
            MemSq(new int[] { 1, 2, 3, 4 }, 2));

            Console.WriteLine("NumTest {0}",
            MemSq(new int[] { 1, 2, 3, 4 }, 3));
        }

    }
}
