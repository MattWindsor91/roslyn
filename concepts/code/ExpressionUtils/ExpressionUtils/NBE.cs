using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


// https://dlr.codeplex.com/


// An implementation of Normalization by Evaluation, based on Typeful Normalization by Evaluation, Danvy, Keller & Puesch
// http://www.cs.au.dk/~mpuech/typeful.pdf
// Needs much cleaning up - combines concepts with GADT

namespace NBE
{

    public abstract class Exp
    {
    }

    public  class Val<T> : Exp
    {
    }
    public class VFun<T,U> : Val<Func<T,U>>
    {
        public Func<Val<T>, Val<U>> f;
        public VFun(Func<Val<T>, Val<U>> f) { this.f = f; }
    }

    public class VBase<T> : Val<Base<T>>
    {
        public Base<T> value;
        public VBase(Base<T> value) { this.value = value; }
    }

    public abstract class NF<T>
    {

    }
    public abstract class AT<T>
    {

    }

    public class NAt<T> : NF<Base<T>>
    {
        public AT<Base<T>> value;
        public NAt(AT<Base<T>> value){ this.value = value; }

        public override string ToString() => value.ToString();
    }

    public class NFun<T, U> : NF<Func<T, U>>
    {
        public Func<Val<T>, NF<U>> value;
        public NFun(Func<Val<T>, NF<U>> value) { this.value = value; }

        public override string ToString()
        {
            var v = new Val<T>();
            return "Fun(" + v.ToString() + "," + value(v).ToString() + ")";
        }

            
    }
 
    public class AApp<T,U> : AT<U>
    {
        public AT<Func<T, U>> f;
        public NF<T> a;
        public AApp(AT<Func<T, U>> f, NF<T> a) { this.f = f; this.a = a; }

        public override string ToString() => f.ToString() + "" + a.ToString();
    }

    public class AVar<T> : AT<T>
    {
        public Val<T> value;
        public AVar(Val<T> value) { this.value = value; }

        public override string ToString() => value.ToString();
    }

    public abstract class Base<T>
    {
        
    }


    public class Atom<T> : Base<T>
    {
        public AT<Base<T>> atom;
        public Atom(AT<Base<T>> atom) { this.atom = atom; } 
    }



    public abstract class Exp<T> : Exp
    {
        public abstract Val<T> Eval();
    }


 
    public class Var<T> : Exp<T>
    {

        Val<T> value;

        public Var(Val<T> value)
        {
            this.value = value;
        }

        public Var() // TBR
        {

        }

        public override Val<T> Eval() => value;



    }

    public class Lam<T, U> : Exp<Func<T, U>>
    {

        public Func<Val<T>, Exp<U>> f;
        public Lam(Func<Val<T>, Exp<U>> f)
        {
            this.f = f;
        }

        public override Val<Func<T, U>> Eval() =>
            new VFun<T, U>(x => f(x).Eval());
    }

    public class App<T, U> : Exp<U>
    {
        public Exp<Func<T, U>> f;
        public Exp<T> e;
        public App(Exp<Func<T, U>> f, Exp<T> e)
        {
            this.f = f;
            this.e = e;
        }

        public override Val<U> Eval() 
            {
                var fv = (f.Eval() as VFun<T, U>).f; //TODO pattern match instead
                return fv(e.Eval());
            }
    }

    public static class Utils
    {


        public static Exp<Func<T, U>> Lam<T, U>(Func<Val<T>, Exp<U>> f) =>
               new Lam<T, U>(f);

        public static Exp<U> Apply<T, U>(this Exp<Func<T, U>> f, Exp<T> e) =>
              new App<T, U>(f, e);

        public static Exp<Func<T, V>> Compose<T, U, V>(Exp<Func<U, V>> f, Exp<Func<T, U>> g) => Lam<T, V>(x => f.Apply(g.Apply(new Var<T>(x))));

        public static Exp<Func<T, T>> Pow<T>(Exp<Func<T, T>> f, int n) => (n > 0) ? Compose(f, Pow(f, n - 1)) : Lam<T, T>(x => new Var<T>(x));

    }

   
    public concept Nbe<A>
    {
        NF<A> Reify(Val<A> a);
        Val<A> Reflect(AT<A> ea);
    }

    public instance NbeBase<T> : Nbe<Base<T>>
    {
        NF<Base<T>> Reify(Val<Base<T>> v) 
            {
                var a = (v as VBase<T>).value;
                var r = (a as Atom<T>).atom;
                return new NAt<T>(r);
            }

        Val<Base<T>> Reflect(AT<Base<T>> r)
             {
                return new VBase<T>(new Atom<T>(r));
            }
   
    }

    public instance NbeFunc<A, B, implicit NbeA, implicit NbeB> : Nbe<Func<A, B>>
        where NbeA : Nbe<A>
        where NbeB : Nbe<B>
    {
        NF<Func<A, B>> Reify(Val<Func<A, B>> v)
        {   var f = (v as VFun<A, B>).f;
            return
                  new NFun<A, B>((Val<A> x) => Reify(f(Reflect(new AVar<A>(x))))); }

        Val<Func<A, B>> Reflect(AT<Func<A, B>> a) =>
            new VFun<A, B>(x => NbeB.Reflect(new AApp<A,B>(a,Reify(x))));
    }

    public static class NbeUtils
    {
        public static NF<A> Nbe<A, implicit NbeA>(Exp<A> a) where NbeA : Nbe<A> => Reify(a.Eval());
    }
  

    public static class Test
    {
        static void Main()
        {
            var e1 = new Lam<Base<int>,Base<int>>(x => new Var<Base<int>>(x));
          
            var e = Utils.Compose(e1, e1);
            var e2 = Utils.Compose(e, e);

            var nf2 = NbeUtils.Nbe(e2);

            var nfs = nf2.ToString();

 
            System.Console.WriteLine(nfs);

            System.Console.ReadLine();

        }



    }


}
