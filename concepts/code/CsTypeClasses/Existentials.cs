// Encoding constraint bounded first-class existentials
// Based on JavaGI, Section 2.7.

namespace Existentials {
  using System;
  using System.Collections.Generic;
 
  interface Showable<A> {
    string Show(A a);
  }

  struct ShowableInt : Showable<int> {
    public string Show(int a) {
      return a.ToString();
    }
  }

  struct ShowableString : Showable<string> {
    public string Show(string a) {
      return a;
    }
  }

  // ExistsShowable is the encoding of JavaGI's "exits A where A implements Showable.A"
  // We first notionally encode this is as
  // "Exists<ShowableX,X> X where ShowableX: struct, Showable<X>"
  // by introducing a second quantified variable, the dictionary "ShowableX".
  // Now, since we don't have first-class existentials, we could translate this using 
  // the universal encoding of existentials
  // "Forall R. (Forall<ShowableX,X>where ShowableX:struct,Showable<X>.X -> R)-> R."
  // But we don't really need the return type R, since we have side-effects, and can just replace R by unit,
  // yielding:
  // "(Forall<ShowableX,X>where ShowableX:struct,Showable<X>.X -> unit) -> unit"
  // Now we can use an interface ExistsShowableClient to encode the nested universal type on the left:
  // ExistsShowableClient ~ Forall<ShowableX,X>where ShowableX:struct,Showable<X>.X -> unit)
  // and the entire existential is encoding as the interface "ExistShowable", that given an
  // an ExistsShowableClient c to method "ExistsShowable.Unpack" (ignore method "UnboxedUnpack" for now), returns void.
  // An existential *value* is encoded as the value "new ExistsShowable<ShowableA,A>(a)" (NB where ShowableA:struct,Showable<A>).
  // The struct ExistsShowable<ShowableA,A> implements the existential interface ExistsShowable by implementing an unpack method that
  // supplies ShowableA, A and a to the client's continuation method, discharging the client to return void.

  // I could have used a superclass and subclass for ExistsShowable and ExistsShowable<,>, but chose an interface and
  // and struct to avoid boxing. Excuse the overloading.
  
  // Voila.
  
  // So what's UnboxedUnpack all about?
  // Unpack(ExistsShowableClient c) just takes a boxed client c of reference type ExistsShowableClient, which forces
  // the client to be heap allocated (just like the environment of an iterator is).
  // UnboxedUnpack is an optimization designed to avoid boxing the client.
  // UnboxedUnpack<E>(ref E c) takes the location of an unboxed client c of type E where E : struct, ExistsShowableClient.
  // UnboxedUnpack can save on the expense of heap-allocating the actual instance implementing ExistsShowableClient.
  // For a comparison, see the boxed and unboxed versions of the client code in class Test below.

  interface ExistsShowableClient{
      void Continue<ShowableA,A>(A a) where ShowableA: struct, Showable<A>;
  }

  interface ExistsShowable  {
    void Unpack(ExistsShowableClient c);
    // this version is more efficient, since it lets us pass an unboxed struct as an client.
    // Note c must be passed by reference to share state, otherwise we'll lose any side-effects on the value of c.
    void UnboxedUnpack<E>(ref E c) where E : struct, ExistsShowableClient;
  }
  
  struct ExistsShowable<ShowableA,A> : ExistsShowable
    where ShowableA: struct, Showable<A>
  {
    private A a;
    public ExistsShowable(A a) { this.a = a; }
    public void Unpack(ExistsShowableClient c) {
      c.Continue<ShowableA,A>(a);
    }
    public void UnboxedUnpack<E>(ref E c) where E : struct, ExistsShowableClient {
      c.Continue<ShowableA, A>(a);
    }
  }
  
  class Test {

    private class CallShowable : ExistsShowableClient 
    {
      internal string res;
       void ExistsShowableClient.Continue<ShowableA, A>(A a) {
        res += default(ShowableA).Show(a);
        res += ",";
      }
    }

    static string Concat(List<ExistsShowable> l) {
      CallShowable env = new CallShowable();
      env.res = "";
      foreach (ExistsShowable x in l) {
        x.Unpack(env);
      };
      return env.res;
    }

    static string ComputeString() {
      List<ExistsShowable> l = new List<ExistsShowable>();
      l.Add(new ExistsShowable<ShowableString, string>("abc"));
      l.Add(new ExistsShowable<ShowableInt, int>(1));
      return Concat(l);
    }


    private struct UnboxedCallShowable : ExistsShowableClient {
      internal string res;
      void ExistsShowableClient.Continue<ShowableA, A>(A a) {
        res += default(ShowableA).Show(a);
        res += ",";
      }
    }

    static string UnboxedConcat(List<ExistsShowable> l) {
      UnboxedCallShowable env = default(UnboxedCallShowable); // this is a struct, so unboxed.
      env.res = "";
      foreach (ExistsShowable x in l) {
        x.UnboxedUnpack(ref env);
      };
      return env.res;
    }

    static string UnboxedComputeString() {
      List<ExistsShowable> l = new List<ExistsShowable>();
      l.Add(new ExistsShowable<ShowableString, string>("abc"));
      l.Add(new ExistsShowable<ShowableInt, int>(1));
      return UnboxedConcat(l);
    }

    public static void Run() {
      System.Console.WriteLine("ComputeString()={0}",ComputeString());
      System.Console.WriteLine("ComputeString()={0}",UnboxedComputeString());
    }
    
  }




}


