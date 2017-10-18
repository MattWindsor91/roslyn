using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Coercions
{

    concept Coercable<T, U>
    {
        U Coerce(T This);
    }

    instance Refl<T> : Coercable<T, T>
    {
        T Coerce(T This) => This;
    }

    instance Trans<T, U, V,implicit WTU, implicit WUV> : Coercable<T, V> 
        where WTU : Coercable<T, U> 
        where WUV : Coercable<U, V>
    {
        V Coerce(T This) => WUV.Coerce(WTU.Coerce(This));
    }

    instance ArrayCoerce<T, U, implicit WTU> : Coercable<T[], U[]>
        where WTU : Coercable<T, U>
    {
        U[] Coerce(T[] This)
        {
            if (This == null) return null;
            var that = new U[This.Length];
            for (int i = 0; i < that.Length; i++)
            {
                that[i] = Coerce(This[i]);
            }
            return that;
        }
    }

    instance FuncCoerce<T1, T2,U1,U2, implicit WT, implicit WU> : Coercable<Func<T1,U1>, Func<T2,U2>>
       where WT : Coercable<T2, T1>
       where WU : Coercable<U1, U2>
       
    {
        Func<T2, U2> Coerce(Func<T1, U1> f) => t2 =>
            Coercable<U1, U2>.Coerce(f(Coercable<T2, T1>.Coerce(t2)));
    }

    instance CoerceFloatDouble : Coercable<float, double>
    {
        double Coerce(float This) => (double) This;
    }

    instance CoerceBoolFloat : Coercable<bool, float>
    {
        float Coerce(bool This) => This? 1.0f : 0.0f ;
    }

    class Program
    {
        static void Main(string[] args)
        {
            float f = 0.1f;
            var d2 = Coercable<bool, float>.Coerce(true);
            //var d2 = Coercable<bool, double>.Coerce(true); transitivity fails! BUG?
            var d = Coercable<float,double>.Coerce(f);
            float[] fa = new float[] { f };
            var da = Coercable<float[], double[]>.Coerce(fa);
            var fd1 = Coercable<Func<float, float>, Func<float, double>>.Coerce(x => x);
            var fd2 = Coercable<Func<double, float>, Func<float, double>>.Coerce(x => (float)x);
        }
    }
}
