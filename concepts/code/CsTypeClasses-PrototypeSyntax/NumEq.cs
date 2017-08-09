// encoding Haskell subclassing...
// overloading numeric operations that also implement equality (thus passing fewer dictionaries to MemSq.
namespace NumEq
{
    using System;

    using Eq;

    concept Num<A> : Eq<A>
    {
        A Add(A a, A b);
        A Mult(A a, A b);
        A Neg(A a);
    }

    instance NumInt : Num<int>
    {
        public bool Equals(int a, int b) => EqInt.Equals(a, b);
        public int Add(int a, int b) => a + b;
        public int Mult(int a, int b) => a * b;
        public int Neg(int a) => -a;
    }

    class Test
    {

        static bool Equals<A, implicit EqA>(A a, A b) where EqA : Eq<A>
        {
            return Equals(a, b);
        }

        static A Square<A, implicit NumA>(A a) where NumA : Num<A>
        {
            return Mult(a, a);
        }

        static bool MemSq<A, implicit NumA>(A[] a_s, A a)
             where NumA : Num<A>
        {
            for (int i = 0; i < a_s.Length; i++)
            {
                if (Equals(a_s[i], Square(a))) return true;
            }
            return false;
        }

        public static void Run()
        {

            Console.WriteLine("NumEqTest {0}",
            MemSq(new int[] { 1, 2, 3, 4 }, 2));

            Console.WriteLine("NumEqTest {0}",
            MemSq(new int[] { 1, 2, 3, 4 }, 3));
        }
    }
}
