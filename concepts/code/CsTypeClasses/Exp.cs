// Laemmel and Ostermanns solution to the expression problem (not that convincing IMHO, but expressible).
// Taken from "Software Extension and Integration with Type Classes".
namespace Exp {
  using System;

  interface Exp<X> { }
  class Lit {
    public int i;
    public Lit(int i) {
      this.i = i;
    }
  }

  class Add<X, Y> {
    public X x;
    public Y y;
    public Add(X x, Y y) {
      this.x = x; this.y = y;
    }
  }

  struct ExpLit : Exp<Lit> {
  }

  struct ExpAdd<ExpX, ExpY, X, Y> : Exp<Add<X, Y>>
    where ExpX : struct, Exp<X>
    where ExpY : struct, Exp<Y> {
  }

  interface Eval<X> : Exp<X> {
    int eval(X x);
  }

  static partial class Overloads {
    public static int eval<EvalX, X>(X e) where EvalX : struct, Eval<X> {
      return default(EvalX).eval(e);
    }
  }
  struct EvalLit : Eval<Lit> {
    public int eval(Lit x) {
      return x.i;
    }
  }
  struct EvalAdd<EvalX, EvalY, X, Y> : Eval<Add<X, Y>>
    where EvalX : struct, Eval<X>
    where EvalY : struct, Eval<Y> {
    public int eval(Add<X, Y> a) {
      return Overloads.eval<EvalX, X>(a.x) + Overloads.eval<EvalY, Y>(a.y);
    }
  }

  class Neg<X> {
    public X x;
    public Neg(X x) {
      this.x = x;
    }
  }

  struct ExpNeg<ExpX, X> : Exp<Neg<X>>
    where ExpX : struct, Exp<X> {
  }

  struct EvalNeg<EvalX, X> : Eval<Neg<X>>
    where EvalX : struct, Eval<X> {
    public int eval(Neg<X> n) {
      return -Overloads.eval<EvalX, X>(n.x);
    }
  }

  interface Print<X> : Exp<X> {
    void print(X x);
  }

  static partial class Overloads {
    public static void Print<PrintX, X>(X e) where PrintX : struct, Print<X> {
      default(PrintX).print(e);
    }
  }

  struct PrintLit : Print<Lit> {
    public void print(Lit x) {
      Console.Write(x.i);
    }
  }

  struct PrintAdd<PrintX, PrintY, X, Y> : Print<Add<X, Y>>
    where PrintX : struct, Print<X>
    where PrintY : struct, Print<Y> {

    public void print(Add<X, Y> a) {
      Overloads.Print<PrintX, X>(a.x);
      Console.Write("+");
      Overloads.Print<PrintY, Y>(a.y);
    }
  }


  class Test {

    public static int Eval<EvalX, X>(X x) where EvalX : struct, Eval<X> {
      return default(EvalX).eval(x);
    }

    public static void Run() {
      Lit one = new Lit(1);
      Add<Lit, Lit> oneplusone = new Add<Lit, Lit>(one, one);
      int two = Eval<EvalAdd<EvalLit, EvalLit, Lit, Lit>, Add<Lit, Lit>>(oneplusone);
      Console.Write("Print(\"1+1\")=\"");
      Overloads.Print<PrintAdd<PrintLit, PrintLit, Lit, Lit>, Add<Lit, Lit>>(oneplusone);
      Console.WriteLine("\"");
      Console.WriteLine("Eval(\"1+1\")={0}", two);
    }
  }
}
