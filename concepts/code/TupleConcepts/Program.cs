using System;
using System.Collections.Generic;
using System.Concepts;

/// <summary>
///     Wrapper instances for tuples.
///     (Probably not that useful, but still a fairly cute example,
///     and exercises our nascent associated types support.)
/// </summary>
namespace TupleConcepts
{
    /// <summary>
    ///     Concept for things that can be treated as a 2-tuple.
    /// </summary>
    /// <typeparam name="I">
    ///     The tuple itself.
    /// </typeparam>
    /// <typeparam name="T1">
    ///     The type of the first item; associated.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of the second item; associated.
    /// </typeparam>
    public concept Tuple2<I, [AssociatedType] T1, [AssociatedType] T2>
    {
        /// <summary>
        ///     Gets the first item of the tuple.
        /// </summary>
        /// <param name="from">
        ///    The tuple.
        /// </param>
        /// <returns>
        ///     The first item of the tuple.
        /// </returns>
        T1 Get1(I from);

        /// <summary>
        ///     Gets the second item of the tuple.
        /// </summary>
        /// <param name="from">
        ///    The tuple.
        /// </param>
        /// <returns>
        ///     The second item of the tuple.
        /// </returns>
        T2 Get2(I from);

        /// <summary>
        ///     Constructs a tuple from two items.
        /// </summary>
        /// <param name="one">
        ///     The first item.
        /// </param>
        /// <param name="two">
        ///     The second item.
        /// </param>
        /// <returns>
        ///     A 2-tuple ontaining the given two items.
        /// </returns>
        I Make(T1 one, T2 two);
    }

    /// <summary>
    ///     Instance of <see cref="Tuple2{T1, T2}"/> for
    ///     <see cref="System.Tuple{T1, T2}"/>.
    /// </summary>
    /// <typeparam name="T1">
    ///     The type of the first item.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of the second item.
    /// </typeparam>
    public instance Tuple2ST<T1, T2> : Tuple2<Tuple<T1, T2>, T1, T2>
    {
        T1 Get1(Tuple<T1, T2> from) => from.Item1;
        T2 Get2(Tuple<T1, T2> from) => from.Item2;
        Tuple<T1, T2> Make(T1 one, T2 two) => Tuple.Create(one, two);
    }

    /// <summary>
    ///     Instance of <see cref="Tuple2{T1, T2}"/> for
    ///     <see cref="System.ValueTuple{T1, T2}"/>.
    /// </summary>
    /// <typeparam name="T1">
    ///     The type of the first item.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of the second item.
    /// </typeparam>
    public instance Tuple2SVT<T1, T2> : Tuple2<(T1, T2), T1, T2>
    {
        T1 Get1((T1, T2) from) => from.Item1;
        T2 Get2((T1, T2) from) => from.Item2;
        (T1, T2) Make(T1 one, T2 two) => (one, two);
    }

    /// <summary>
    ///     Instance of <see cref="Tuple2{T1, T2}"/> for
    ///     <see cref="System.Collections.Generic.KeyValuePair{T1, T2}"/>.
    /// </summary>
    /// <typeparam name="T1">
    ///     The type of the first item.
    /// </typeparam>
    /// <typeparam name="T2">
    ///     The type of the second item.
    /// </typeparam>
    public instance Tuple2SKVP<T1, T2> : Tuple2<KeyValuePair<T1, T2>, T1, T2>
    {
        T1 Get1(KeyValuePair<T1, T2> from) => from.Key;
        T2 Get2(KeyValuePair<T1, T2> from) => from.Value;
        KeyValuePair<T1, T2> Make(T1 one, T2 two) => new KeyValuePair<T1, T2>(one, two);
    }

    /// <summary>
    ///     Concept for things that can be turned into strings.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the thing to show.
    /// </typeparam>
    public concept Showable<T>
    {
        /// <summary>
        ///     Converts a showable into a string.
        /// </summary>
        /// <param name="t">
        ///     The thing to show.
        /// </param>
        /// <returns>
        ///     The human-readable string representation of <paramref name="t"/>.
        /// </returns>
        string Show(T t) => t.ToString();
    }

    /// <summary>
    ///     Instance of <see cref="Showable{T}"/> for integers.
    /// </summary>
    public instance ShowInt : Showable<int>
    {
        string Show(int t) => t.ToString();
    }

    /// <summary>
    ///     Instance of <see cref="Showable{T}"/> for 2-tuples.
    /// </summary>
    public instance ShowT2<I, [AssociatedType] T1, [AssociatedType] T2, implicit T2I, implicit ST1, implicit ST2> : Showable<I>
        where T2I : Tuple2<I, T1, T2>
        where ST1 : Showable<T1>
        where ST2 : Showable<T2>
    {
        string Show(I t) => $"({Show(Get1(t))}, {Show(Get2(t))})";
    }

    /// <summary>
    /// Test program for tuple concepts.
    /// </summary>
    class Program
    {
        static string Show<A, implicit SA>(A a) where SA : Showable<A> => Show(a);

        static void Main(string[] args)
        {
            var tuple = Tuple2<Tuple<int, int>>.Make(66, 99);
            Console.Out.WriteLine($"Tuple: {Show(tuple)}");

            var valueTuple = (27, 53);
            Console.Out.WriteLine($"ValueTuple: {Show(valueTuple)}");

            var dict = new Dictionary<int, int>();
            dict.Add(1, 1997);
            dict.Add(2, 2001);
            dict.Add(3, 2005);
            foreach (KeyValuePair<int, int> kvp in dict)
            {
                Console.Out.WriteLine($"KeyValuePair: {Show(kvp)}");
            }
        }
    }
}
