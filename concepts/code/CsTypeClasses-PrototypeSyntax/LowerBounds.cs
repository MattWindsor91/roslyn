//
// TODO: We don't support variance yet, so this hasn't been sugared.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsTypeClasses
{
    // encoding lower bounds (way cool me thinks)

    interface Below<in A, out B> {
        // note that A must be contra-variant for List<out A> to type check.
        // interestingly this typechecks even if B is invariant, not covariant.

        B Cast(A a);
    }

    struct Cast<A, B> : Below<A, B>
           where A : B

    {
        B Below<A, B>.Cast(A a) { return a; }
    }




    interface List<out A>
    {

        List<B> Append<B, Cast>(B other) where Cast : struct, Below<A, B>;

        List<B> Append<B, Cast>(List<B> other) where Cast : struct, Below<A, B>;

    }

    class Nil<A> : List<A>
    {

        List<B> List<A>.Append<B, Cast>(B b)
            => new Cons<B>(b, new Nil<B>());


        List<B> List<A>.Append<B, Cast>(List<B> bs)
            => bs;

    }

    class Cons<A> : List<A>
    {
        public A h;
        public List<A> t;


        public Cons(A h, List<A> t)
        {
            this.h = h; this.t = t;
        }



        List<B> List<A>.Append<B, Cast>(B b)
            => new Cons<B>(default(Cast).Cast(this.h), this.t.Append<B, Cast>(b));


        List<B> List<A>.Append<B, Cast>(List<B> bs)
            => new Cons<B>(default(Cast).Cast(this.h), this.t.Append<B, Cast>(bs));
    }

    class Test {

        public static void Test1() {

            List<string> os1 = new Cons<string>("hello",new Cons<string>("World",new Nil<string>())) ;

            List<object> os2 = new Cons<object>(1, new Cons<object>(2, new Nil<object>()));

            var os11 = os1.Append<object, Cast<string, object>>(os1);

            var os12= os1.Append<object, Cast<string, object>>(os2);

            var os21 = os2.Append<object, Cast<object, object>>(os1);

        }





    }




}
