using System;
using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Enumerable;

namespace TinyLinq
{
    // TODO: generalise and make lazy

    public concept CSelectMany<[AssociatedType] T, [AssociatedType] U, [AssociatedType] V, CT, [AssociatedType] CU, [AssociatedType] CV>
    {
        CV SelectMany(CT src, Func<T, CU> selector, Func<T, U, V> resultSelector);
    }

    public instance ListSelectMany<T, U, V> : CSelectMany<T, U, V, List<T>, List<U>, List<V>>
    {
        List<V> SelectMany(List<T> src, Func<T, List<U>> selector, Func<T, U, V> resultSelector)
        {
            var vs = new List<V>();
            foreach (T t in src)
            {
                var us = selector(t);
                foreach (U u in us)
                    vs.Add(resultSelector(t, u));
            }
            return vs;
        }
    }

    public instance ArraySelectMany<T, U, V> : CSelectMany<T, U, V, T[], U[], V[]>
    {
        V[] SelectMany(T[] src, Func<T, U[]> selector, Func<T, U, V> resultSelector)
        {
            var vs = new List<V>(); // rather inefficient
            foreach (T t in src)
            {
                var us = selector(t);
                foreach (U u in us)
                    vs.Add(resultSelector(t, u));
            }
            return vs.ToArray();
        }
    }
}
