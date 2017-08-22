using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Concepts;


namespace TinyLinq
{

    
    concept CSelect<[AssociatedType] T, [AssociatedType] U, S, [AssociatedType] D>
    {
        D Select(S src, Func<T, U> f);
    }

    instance ListSelect<T, U> : CSelect<T, U, List<T>, List<U>>
    {
        List<U> Select(List<T> src, Func<T, U> f)
        {
            var l = new List<U>(src.Capacity);
            foreach (var e in src)
                l.Add(f(e));
            return l;
        }
    }



    instance ArraySelect<T, U> : CSelect<T, U, T[], U[]>
    {
        U[] Select(T[] src, Func<T, U> f)
        {
            var l = new U[src.Length];
            for (int i = 0; i < src.Length; i++)
                l[i] = f(src[i]);
            return l;
        }
    }


    concept CWhere<[AssociatedType] T, S>
    {
        S Where(S src, Func<T, bool> f);
    }

    instance ListWhere<T> : CWhere<T, List<T>>
    {
        List<T> Where(List<T> src, Func<T, bool> f)
        {
            var l = new List<T>(src.Capacity);
            foreach (var e in src)
                if (f(e)) l.Add(e);
            return l;
        }
    }

    instance ArrayWhere<T> : CWhere<T, T[]>
    {
        T[] Where(T[] src, Func<T, bool> f)
        {
            var l = new List<T>(src.Length); // rather inefficient
            foreach (var e in src)
                if (f(e)) l.Add(e);
            return l.ToArray();
        }
    }

   

    static class Extension
    {
        /*
        public static List<U> Select<T, U>(this List<T> This, Func<T, U> f)
        {
            return null;

        }*/

       /*
        // too generic
        public static D Select<[AssociatedType]T, [AssociatedType]U, S, [AssociatedType] D, implicit M>(this S This, Func<T, U> f) where M : CSelect<T, U, S, D>
        {
            return M.Select(This, f);
        }
        */


        // works
        public static D Select<[AssociatedType]T, [AssociatedType]U, [AssociatedType] D, implicit M>(this List<T> This, Func<T, U> f) where M : CSelect<T, U, List<T>, D>
        {
            return M.Select(This, f);
        }


        // works
        public static D Select<[AssociatedType]T, [AssociatedType]U, [AssociatedType] D, implicit M>(this T[] This, Func<T, U> f) where M : CSelect<T, U, T[], D>
        {
            return M.Select(This, f);
        }



        // works
        public static List<T> Where<[AssociatedType]T, implicit M>(this List<T> This, Func<T, bool> f) where M : CWhere<T, List<T>>
        {
            return M.Where(This, f);
        }


        // works
        public static T[] Where<[AssociatedType]T, implicit M>(this T[] This, Func<T, bool> f) where M : CWhere<T, T[]>
        {
            return M.Where(This, f);
        }


    }



    class Program
    {
        static void Main(string[] args)
        {
            List<int> l = new List<int>(new int[] { 1, 2, 3 });

            List<double> l1 = from x in l where x % 2 == 0 select (double) x;

            int[] a = new int[] { 1, 2, 3 };
            double[] a1 = from x in a where x % 2 == 0  select (double) x;

        }




    }
}
   
