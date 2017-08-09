// encoding Haskell subclassing...
// overloading numeric operations that also implement equality (thus passing fewer dictionaries to MemSq.
namespace NumEq {
  using System;

  using Eq;
   
  interface Num<A> : Eq<A> {
    A Add(A a, A b);
    A Mult(A a, A b);
    A Neg(A a);
  }

  struct NumInt : Num<int> {
    public bool Equals(int a, int b) { return default(EqInt).Equals(a, b); }
    public int Add(int a, int b) { return a + b; }
    public int Mult(int a, int b) { return a * b; }
    public int Neg(int a) { return -a; }
  }

  class Test {

    static bool Equals<EqA, A>(A a, A b) where EqA : struct, Eq<A> {
      EqA eqA = default(EqA);
      return eqA.Equals(a, b);
    }

    static A Square<NumA, A>(A a) where NumA : struct, Num<A> {
      return default(NumA).Mult(a, a);
    }

    static bool MemSq<NumA, A>(A[] a_s, A a)
         where NumA : struct, Num<A> {
      for (int i = 0; i < a_s.Length; i++) {
        if (Equals<NumA, A>(a_s[i], Square<NumA, A>(a))) return true;
      }
      return false;
    }

    public static void Run() {

      Console.WriteLine("NumEqTest {0}",
      MemSq<NumInt, int>(new int[] { 1, 2, 3, 4 }, 2));

      Console.WriteLine("NumEqTest {0}",
      MemSq<NumInt, int>(new int[] { 1, 2, 3, 4 }, 3));
    }
  }
}
