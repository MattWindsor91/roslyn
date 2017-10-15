using System;
using System.Concepts;

namespace Defaults
{
    // # Defaults
    //
    // Sometimes, concept members have an obvious 'default behaviour'
    // that should be inferred whenever an instance doesn't specify
    // its own.  Concept-C# lets concept members provide a body that
    // is automatically used whenever the member is missed out of an
    // instance definition *and* there is no suitable autofill for it.

    class Program
    {
        concept Eq<A>
        {
            // By adding a body to Eq, we specify the body as the
            // default implementation of Eq for any instance that doesn't
            // have its own.
            bool Eq(A a, A b) => !Neq(a, b);

            // Defaults work with block bodies, too.
            bool Neq(A a, A b) { return !Eq(a, b); }
        }

        // Here's another example of defaults:

        concept Show<A>
        {
            string Show(A a);
            void Println(A a) => Console.Out.WriteLine(Show(a));
        }

        instance EqInt : Eq<int>
        {
            // Here, we only define Eq, not Neq.
            bool Eq(int a, int b) => a == b;
        }

        instance ShowBool : Show<bool>
        {
            // Here, we only define Show, not Println.
            string Show(bool a) => a ? "yes" : "no";
        }

        static void Main(string[] args)
        {
            Show<bool>.Println(EqInt.Eq(27, 53));

            // Concept defaults are installed per-instance, so we can still
            // directly call into ShowBool.Println and get the default
            // behaviour:
            ShowBool.Println(EqInt.Neq(27, 53));
        }
    }
}
