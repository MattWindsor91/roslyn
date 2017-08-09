// overloading numeric operations
namespace Num {
  using System;
  using Eq;
  interface Num<A> {
    A Add(A a, A b);
    A Mult(A a, A b);
    A Neg(A a);
  }

  static class Overloads {
    public static A Add<NumA, A>(A a, A b) where NumA : struct, Num<A> {
      return default(NumA).Add(a, a);
    }

    public static A Mult<NumA, A>(A a, A b) where NumA : struct, Num<A> {
      return default(NumA).Mult(a, a);
    }

    public static A Neg<NumA, A>(A a) where NumA : struct, Num<A> {
      return default(NumA).Neg(a);
    }
  }

  struct NumInt : Num<int> {
    int Num<int>.Add(int a, int b) { return a + b; }
    int Num<int>.Mult(int a, int b) { return a * b; }
    int Num<int>.Neg(int a) { return -a; }
  }

  class Test {

    static A Square<NumA, A>(A a) where NumA : struct, Num<A> {
      return Overloads.Mult<NumA, A>(a, a);
    }

    static bool MemSq<NumA, EqA, A>(A[] a_s, A a)
      where NumA : struct, Num<A>
      where EqA : struct, Eq<A> {
      for (int i = 0; i < a_s.Length; i++) {
        if (Eq.Overloads.Eq<EqA, A>(a_s[i], Square<NumA, A>(a))) {
          return true;
        }
      }
      return false;
    }


    public static void Run() {

      Console.WriteLine("NumTest {0}",
      MemSq<NumInt, EqInt, int>(new int[] { 1, 2, 3, 4 }, 2));

      Console.WriteLine("NumTest {0}",
      MemSq<NumInt, EqInt, int>(new int[] { 1, 2, 3, 4 }, 3));
    }

  }
}
