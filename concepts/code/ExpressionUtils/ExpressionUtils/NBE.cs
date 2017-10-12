using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


// https://dlr.codeplex.com/

namespace NBE
{
    using static Utils;



    using Env = Func<Exp, ParameterExpression>;
    using Map = Func<Exp, Exp>;


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

        public abstract Expression Translate(Env E);

        public Func<T> Compile()
        {
            var c = this.Translate(Empty);
            var l = Expression.Lambda<Func<T>>(c);
            return l.Compile();
        }

        public T Run() => Compile()();

        public virtual Exp<T> Reduce(Map M) { return this; }


        public virtual Val<T> Eval()
        {
            throw new System.NotImplementedException();
        }
    }


    public class Constant<T> : Exp<T>
    {
        public T c;
        public Constant(T c)
        {
            this.c = c;
        }

        public override Expression Translate(Env E)
        {
            return Expression.Constant(c);
        }
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

        public override Expression Translate(Env E)
        {
            return E(this);
        }

        public override Exp<T> Reduce(Map M)
        {
            return (Exp<T>)M(this);
        }
        public override Val<T> Eval() => value;
      



    }

    public class Lam<T, U> : Exp<Func<T, U>>
    {
        // public Var<T> v;
        // public Exp<U> e;
        public Func<Val<T>, Exp<U>> f;
        public Lam(Func<Val<T>, Exp<U>> f)
        {
            this.f = f;
        }

        public override Expression Translate(Env E)
        {
            var p = Expression.Parameter(typeof(T));
            var v = new Val<T>();
            var Ex = E.Add(v, p);
            return Expression.Lambda(f(v).Translate(Ex), p);
        }

        public override Exp<Func<T, U>> Reduce(Map M)
        {
            var v = new Val<T>();
            return Lam<T, U>(x => f(v).Reduce(M.Add(v, x)));
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

        public override Expression Translate(Env E)
        {
            return Expression.Invoke(f.Translate(E), e.Translate(E));
        }


        public override Exp<U> Reduce(Map M)
        {
            var fr = f.Reduce(M) as Exp<Func<T, U>>;
            var er = e.Reduce(M) as Exp<T>;
            var lambda = fr as Lam<T, U>;
            if (lambda == null)
            {
                return fr.Apply(er);
            }
            else
            {
                var v = new Val<T>();
                return Let(er, x => (lambda.f(v)).Reduce(M.Add(v, x)));
            }
        }

        public override Val<U> Eval() 
            {
                var fv = (f.Eval() as VFun<T, U>).f; //TODO pattern match instead
                return fv(e.Eval());
            }
    }



    public class Let<T, U> : Exp<U>
    {
        
        public Exp<T> e;
        public Func<Var<T>, Exp<U>> f;
        public Let(Exp<T> e, Func<Var<T>, Exp<U>> f)
        {
            this.e = e;
            this.f = f;
        }

        public override Expression Translate(Env E)
        {
            var p = Expression.Parameter(typeof(T));
            var ce = e.Translate(E);
            var x = new Var<T>();
            var Ex = E.Add(x, p);
            var fc = f(x).Translate(Ex);
            return Expression.Block(new[] { p }, Expression.Assign(p, ce), fc);
        }

        public override Exp<U> Reduce(Map M)
        {
            var x = new Var<T>();
            return Let(e.Reduce(M), y => f(x).Reduce(M.Add(x, y)));
        }


    }


    public class Prim<T1, T> : Exp<T>
    {

        public Expression<Func<T1, T>> f;
        public Exp<T1> e1;

        public Prim(Expression<Func<T1, T>> f, Exp<T1> e1)
        {
            this.f = f;
            this.e1 = e1;
        }

        public override Expression Translate(Env E)
        {
            var p = f.Parameters[0];
            var c1 = e1.Translate(E);
            return Expression.Block(new[] { p },
                                    Expression.Assign(p, c1),
                                    f.Body);
        }

        public override Exp<T> Reduce(Map M)
        {
            return Prim(f, e1.Reduce(M));
        }
    }

    public class Prim<T1, T2, T> : Exp<T>
    {

        public Expression<Func<T1, T2, T>> f;
        public Exp<T1> e1;
        public Exp<T2> e2;
        public Prim(Expression<Func<T1, T2, T>> f, Exp<T1> e1, Exp<T2> e2)
        {
            this.f = f;
            this.e1 = e1;
            this.e2 = e2;
        }

        public override Expression Translate(Env E)
        {
            var p = f.Parameters[0];
            var q = f.Parameters[1];
            var c1 = e1.Translate(E);
            var c2 = e2.Translate(E);
            return Expression.Block(new[] { p, q },
                                    Expression.Assign(p, c1),
                                    Expression.Assign(q, c2),
                                    f.Body);
        }

        public override Exp<T> Reduce(Map M)
        {
            return Prim(f, e1.Reduce(M), e2.Reduce(M));
        }
    }



    public static class Utils
    {

        public static Env Empty = x => { throw new System.ArgumentOutOfRangeException(); };
        public static Map EmptyMap = x => x;

        public static Env Add(this Env E, Exp x, ParameterExpression p) =>
                       (y) => (x == y) ? p : E(x);

        public static Map Add(this Map E, Exp x, Exp p) =>
                     (y) => (x == y) ? p : E(x);

        public static Exp<T> C<T>(T t) => new Constant<T>(t);
        public static Exp<Func<T, U>> Lam<T, U>(Func<Val<T>, Exp<U>> f) =>
               new Lam<T, U>(f);


        public static Exp<U> Let<T, U>(Exp<T> e, Func<Var<T>, Exp<U>> f) =>
               (e is Var<T>) ?
                 f(e as Var<T>)
               : new Let<T, U>(e, f);

        public static Exp<U> Apply<T, U>(this Exp<Func<T, U>> f, Exp<T> e) =>
              new App<T, U>(f, e);

        public static Exp<T> Prim<T1, T2, T>(Expression<Func<T1, T2, T>> f, Exp<T1> e1, Exp<T2> e2) =>
             new Prim<T1, T2, T>(f, e1, e2);
        public static Exp<T> Prim<T1, T>(Expression<Func<T1, T>> f, Exp<T1> e1) =>
             new Prim<T1, T>(f, e1);


        public static Exp<Func<T, V>> Compose<T, U, V>(Exp<Func<U, V>> f, Exp<Func<T, U>> g) => Lam<T, V>(x => f.Apply(g.Apply(new Var<T>(x))));

        public static Exp<Func<T, T>> Pow<T>(Exp<Func<T, T>> f, int n) => (n > 0) ? Compose(f, Pow(f, n - 1)) : Lam<T, T>(x => new Var<T>(x));

    }

    public class Coerce<T> : Exp<T>
    {
        private readonly Exp<Exp<T>> inner;

        public Coerce(Exp<Exp<T>> e)
        {
            inner = e;
        }

        public override Expression Translate(Env E)
        {
            return inner.Run().Translate(E);
        }
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

            
            var e1 = Lam<Base<int>,Base<int>>(x => new Var<Base<int>>(x));
          
            var e = Compose(e1, e1);
            var e2 = Compose(e, e);

            var nf2 = NbeUtils.Nbe(e2);

            var nfs = nf2.ToString();


            System.Console.WriteLine(nfs);


            System.Console.ReadLine();



        }



    }


}
