// prototypal syntax for operator overloading on concepts
namespace OpNum
{
    using System;
    using OpEq;

    concept Num<A>
    {
        A operator +(A a, A b);
        A operator *(A a, A b);
        A operator -(A a);
        A FromInteger(int i);
    }

    instance NumInt : Num<int>
    {
        int operator +(int a, int b) => a + b;
        int operator *(int a, int b) => a * b;
        int operator -(int a) => -a;
        int FromInteger(int i) => i;
    }

    class Test
    {

        static A Square<A, implicit NumA>(A a) where NumA : Num<A>
        {
            return a * a;
        }

        static bool MemSq<A, implicit NumA, implicit EqA>(A[] a_s, A a)
          where NumA : Num<A>
          where EqA : Eq<A>
        {
            for (int i = 0; i < a_s.Length; i++)
            {
                if (a_s[i] == Square(a))
                {
                    return true;
                }
            }
            return false;
        }

        static A SumNegOdd<A, implicit NumA>(A[] a_s)
          where NumA : Num<A>
        {
            A sum = FromInteger(0);
            for (int i = 0; i < a_s.Length; i++)
            {
                sum += ((i % 2 == 0) ? a_s[i] : -(a_s[i]));
            }
            return sum;
        }


        public static void Run()
        {

            Console.WriteLine("OpNumTest {0}",
            MemSq(new int[] { 1, 2, 3, 4 }, 2));

            Console.WriteLine("OpNumTest {0}",
            MemSq(new int[] { 1, 2, 3, 4 }, 3));

            Console.WriteLine("OpNumTest {0}",
            SumNegOdd(new int[] { 1, 2, 3, 4 }));
        }

    }
}
