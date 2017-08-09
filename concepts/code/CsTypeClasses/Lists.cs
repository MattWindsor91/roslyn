// List with a derived equality operation, but its ugly and nonextensible (see ListsEq for something better?).

namespace Lists {
  using System;
  using Eq;

  delegate B Fun<A, B>(A a);

  abstract class List<A> {
    public abstract List<B> Map<B>(Fun<A, B> f);
    public abstract bool Mem<EqA>(A a) where EqA : struct, Eq<A>;

    //a contrived version of Mem that recurses on a list of lists, so needing a dynamically constructed dictionary.
    public abstract bool AltMem<EqA>(A a) where EqA : struct, Eq<A>;
  }

  class Nil<A> : List<A> {
    public override List<B> Map<B>(Fun<A, B> f) {
      return new Nil<B>();
    }

    public override bool Mem<EqA>(A a) {
      return false;
    }

    public override bool AltMem<EqA>(A a) {
      return false;
    }
  }

  class Cons<A> : List<A> {
    public A h;
    public List<A> t;

    public override List<B> Map<B>(Fun<A, B> f) {
      return new Cons<B>(f(h), t.Map(f));
    }

    public override bool Mem<EqA>(A a) {
      return Overloads.Eq<EqA, A>(a, h) || t.Mem<EqA>(a);
      //return Eq(a, h) || t.Mem(a);
    }

    public override bool AltMem<EqA>(A a) {
      Fun<A, List<A>> wrap = delegate(A e) { return new Cons<A>(e, new Nil<A>()); };
      return Overloads.Eq<EqA, A>(a, h) || t.Map(wrap).Mem<EqList<EqA, A>>(wrap(a));
      //return Eq(a,h) || t.Map(wrap).Mem(wrap(a));

      //return Eq<A>.Eq<a,h> || t.Map.(wrap).Mem(wrap(a));
    }

    public Cons(A h, List<A> t) {
      this.h = h; this.t = t;
    }


  }
  struct EqList<EqA, A> : Eq<List<A>> where EqA : struct, Eq<A> {
    bool Eq<List<A>>.Equals(List<A> a, List<A> b) {
      Nil<A> an = a as Nil<A>;
      if (an != null) {
        Nil<A> ab = b as Nil<A>;
        return b != null;
      };
      Cons<A> ac = a as Cons<A>;
      if (ac != null) { 
        Cons<A> bc = b as Cons<A>;
        return (bc != null && Overloads.Eq<EqA, A>(ac.h, bc.h) && Overloads.Eq<EqList<EqA, A>, List<A>>(ac.t, bc.t));
      }
      return b == null;
    }
  }

  static class Test {
    static List<A> FromArray<A>(params A[] a) {
      List<A> l = new Nil<A>();
      for (int i = a.Length - 1; i >= 0; i--) {
        l = new Cons<A>(a[i], l);
      }
      return l;
    }

    public static void Run() {
      List<int> l1 = FromArray(1,2,3,4);
      List<int> l2 = FromArray(5,6,7,8);
      Console.WriteLine("Lists: Eq(null,null)={0}", Overloads.Eq<EqList<EqInt, int>, List<int>>(null, null));
      Console.WriteLine("Lists: Eq(null,l1)={0}", Overloads.Eq<EqList<EqInt, int>, List<int>>(null, l1));
      Console.WriteLine("Lists: Eq(l1,null)={0}", Overloads.Eq<EqList<EqInt, int>, List<int>>(l1,null));
      Console.WriteLine("Lists: Eq(l1,l1)={0}",Overloads.Eq<EqList<EqInt, int>,List<int>>(l1, l1));
      Console.WriteLine("Lists: Eq(l1,l2)={0}", Overloads.Eq<EqList<EqInt, int>,List<int>>(l1, l2));
      List<List<int>> ll1 = FromArray(l1, l2);
      List<List<int>> ll2 = FromArray(l2, l1);

      Console.WriteLine("Lists: Eq(null,null)={0}", Overloads.Eq<EqList<EqList<EqInt, int>, List<int>>, List<List<int>>>(null, null));
      Console.WriteLine("Lists: Eq(null,ll1)={0}", Overloads.Eq<EqList<EqList<EqInt, int>, List<int>>, List<List<int>>>(null, ll1));
      Console.WriteLine("Lists: Eq(ll1,null)={0}", Overloads.Eq<EqList<EqList<EqInt, int>, List<int>>, List<List<int>>>(ll1, null));
      Console.WriteLine("Lists: Eq(ll1,ll1)={0}", Overloads.Eq<EqList<EqList<EqInt, int>,List<int>>, List<List<int>>>(ll1, ll1));
      Console.WriteLine("Lists: Eq(ll1,ll2)={0}", Overloads.Eq<EqList<EqList<EqInt, int>,List<int>>, List<List<int>>>(ll1, ll2));
    }
  }
}