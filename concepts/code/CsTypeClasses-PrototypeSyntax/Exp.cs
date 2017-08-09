// Laemmel and Ostermanns solution to the expression problem (not that convincing IMHO, but expressible).
// Taken from "Software Extension and Integration with Type Classes".
namespace Exp
{
    using System;

    concept Exp<X>
    {
    }

    class Lit
    {
        public int i;

        public Lit(int i)
        {
            this.i = i;
        }
    }

    class Add<X, Y>
    {
        public X x;
        public Y y;

        public Add(X x, Y y)
        {
            this.x = x; this.y = y;
        }
    }

    instance ExpLit : Exp<Lit>
    {
    }

    instance ExpAdd<X, Y, implicit ExpX, implicit ExpY> : Exp<Add<X, Y>>
        where ExpX : Exp<X>
        where ExpY : Exp<Y>
    {
    }

    concept Eval<X> : Exp<X>
    {
        int eval(X x);
    }

    static partial class Overloads
    {
        public static int eval<X, implicit EvalX>(X e) where EvalX : Eval<X> => eval(e);
    }

    instance EvalLit : Eval<Lit>
    {
        int eval(Lit x) => x.i;
    }

    instance EvalAdd<X, Y, implicit EvalX, implicit EvalY> : Eval<Add<X, Y>>
        where EvalX : Eval<X>
        where EvalY : Eval<Y>
    {
        int eval(Add<X, Y> a) => Overloads.eval(a.x) + Overloads.eval(a.y);
    }

    class Neg<X>
    {
        public X x;

        public Neg(X x)
        {
            this.x = x;
        }
    }

    instance ExpNeg<X, implicit ExpX> : Exp<Neg<X>>
        where ExpX : Exp<X>
    {
    }

    instance EvalNeg<X, implicit EvalX> : Eval<Neg<X>>
        where EvalX : Eval<X>
    {
        int eval(Neg<X> n) => -Overloads.eval(n.x);
    }

    concept Print<X> : Exp<X>
    {
        void print(X x);
    }

    static partial class Overloads
    {
        public static void Print<X, implicit PrintX>(X e) where PrintX : Print<X>
        {
            print(e);
        }
    }

    instance PrintLit : Print<Lit>
    {
        void print(Lit x)
        {
            Console.Write(x.i);
        }
    }

    instance PrintAdd<X, Y, implicit PrintX, implicit PrintY> : Print<Add<X, Y>>
        where PrintX : Print<X>
        where PrintY : Print<Y>
    {
        void print(Add<X, Y> a)
        {
            Overloads.Print(a.x);
            Console.Write("+");
            Overloads.Print(a.y);
        }
    }


    class Test
    {
        public static int Eval<X, implicit EvalX>(X x) where EvalX : Eval<X> => eval(x);

        public static void Run()
        {
            Lit one = new Lit(1);
            Add<Lit, Lit> oneplusone = new Add<Lit, Lit>(one, one);
            int two = Eval(oneplusone);
            Console.Write("Print(\"1+1\")=\"");
            Overloads.Print(oneplusone);
            Console.WriteLine("\"");
            Console.WriteLine("Eval(\"1+1\")={0}", two);
        }
    }
}