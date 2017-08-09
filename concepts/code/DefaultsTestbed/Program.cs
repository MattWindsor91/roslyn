using System;
using System.Concepts;

namespace DefaultsTestbed
{
    class Program
    {
        concept Eq<A>
        {
            bool Eq(A a, A b) => !Neq(a, b);
            bool Neq(A a, A b) { return !Eq(a, b); }
        }

        concept Show<A>
        {
            string Show(A a);
            void Println(A a) => Console.Out.WriteLine(Show(a));
        }

        instance EqInt : Eq<int>
        {
            bool Eq(int a, int b) => a == b;
        }

        instance ShowBool : Show<bool>
        {
            string Show(bool a) => a ? "yes" : "no";
        }

        static void Main(string[] args)
        {
            Show<bool>.Println(EqInt.Eq(27, 53));
            ShowBool.Println(EqInt.Neq(27, 53));
        }
    }
}
