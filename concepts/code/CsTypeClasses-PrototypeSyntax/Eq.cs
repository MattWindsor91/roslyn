using System;

namespace Eq
{

    /*
    We represent Haskell type classes as Generic interfaces.

      class Eq a where 
      (==)                  :: a -> a -> Bool

    */

    concept Eq<A>
    {
        bool Equals(A a, A b);
    }

    /*

    The Haskell declaration of class Eq implicitly declares the overloaded operations induced by class Eq a’s members.
      (==)                    :: (Eq a) => a -> a -> Bool

    In CS, have to do this explicitly, for each member.
    An operation over some class is a static generic method, parameterized by both a dictionary type parameter,
    and the constrained type parameter itself. 
    The dictionary is marked "struct" so we can access its virtual operations 
    through a default value heap allocating or raising NullReference exception and withoug passing around
    dictionary values (just types!).

    */

    static class Overloads
    {
        public static bool Eq<A, implicit EqA>(A a, A b) where EqA : Eq<A> => Equals(a, b);
    }


    /*
    A Haskell ground instance, eg.

    instance Eq Integer where 
      x == y                =  x `integerEq` y

    instance Eq Float where
      x == y                =  x `floatEq` y

    is translated to a non-generic struct implementing the appropriate type class interface.
    */

    instance EqInt : Eq<int>
    {
        bool Equals(int a, int b) => a == b;
    }

    instance EqFloat : Eq<float>
    {
        bool Equals(float a, float b) => a == b;
    }


    /*
    We can represent a Haskell parameterized instance as a generic struct, 
    implementing an interface but parameterized by suitably constrained type parameters. 

    instance (Eq a) => Eq ([a]) where 
      nil        == nil                 = true
      (a:as) == (b:bs)                  =  (a == b) && (as == bs)

    This Haskell code defines, given an equality on type a’s (any a) an equality operation on type list of a, written [a].

    Substituting, for simplicity, arrays for lists in CS we can write:
    */

    instance EqArray<A, implicit EqA> : Eq<A[]> where EqA : Eq<A>
    {
        bool Equals(A[] a, A[] b)
        {
            if (a == null) return b == null;
            if (b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!Overloads.Eq(a[i], b[i])) return false;
            return true;
        }
    }


    /* Derived operations

    We translate Haskell’s qualified types as extra type parameters, constrained to be both structs and bound by translations of their type class constraints.

    For example, list membership in Haskell is
    elem :: Eq a => a -> [a] -> bool
    x `elem`  []            = False
    x `elem` (y:ys)         = x==y || (x `elem` ys)  

    In C#, we can define:

    */

    public class Test
    {

        static bool Elem<A, implicit EqA>(A x, A[] ys) where EqA : Eq<A>
        {
            for (int i = 0; i < ys.Length; i++)
            {
                if (Overloads.Eq(x, ys[i])) return true;
            }
            return false;
        }



        /*
        Now every dictionary that implements Ord<A> must implement Eq<A> too, and can be used
        as such when required.

        Of course, in CS we have to make Dictionary construction and passing (as types, not values) explicit. 
        This is what a modified C# compiler would hopefully be able to do for you, 
        just like Haskell does by solving constraints to construct dictionaries at compile time.

        The point is that the underlying mechanism for implementing Haskell Type Classes is already there!

         */

        public static void Run()
        {

            Console.WriteLine("Find Test {0}", Elem(1, new int[] { 1, 2, 3 }));
            Console.WriteLine("Find Test {0}", Elem(4, new int[] { 1, 2, 3 }));

            Console.WriteLine("Equals Test {0}",
            Overloads.Eq(new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } },
                         new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } }));

            Console.WriteLine("Equals Test {0}",
            Overloads.Eq(new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 } },
                         new int[][] { new int[] { 1, 2 }, new int[] { 3, 5 } }));
        }

        /*
        Finally we translate Haskell subclassing  to interface inheritance.
        See file NumEq for an example... */
    }
}

