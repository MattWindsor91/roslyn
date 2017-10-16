using System;
using System.Collections.Generic;
using System.Linq;

// The CSerial concept and ground instances.
//
// CSerial is used to generate inputs whenever we test a forall or exists.
//
// Useful combinators for defining CSerial instances are in the 'SerialHelpers'
// static class.

namespace SerialPBT
{
    /// <summary>
    /// Concept for types that can generate a sequence of values given
    /// a maximum depth.
    /// </summary>
    /// <typeparam name="T">
    /// The type that is serial.
    /// </typeparam>
    public concept CSerial<T>
    {
        /// <summary>
        /// Generates a sequence of values up to the given depth.
        /// </summary>
        /// <param name="depth">
        /// The depth at which to enumerate values.  What this means
        /// in terms of which values are generated depends on the
        /// type.  Depth will always be positive.
        /// </param>
        /// <returns>
        /// An enumerable series of values.
        /// </returns>
        IEnumerable<T> Series(int depth);
    }

    #region Common CSerial instances

    /// <summary>
    /// Serial instance for signed integers, where the depth is taken as the
    /// maximum absolute value.
    /// </summary>
    public instance SerialInt : CSerial<int>
    {
        IEnumerable<int> Series(int depth)
        {
            if (depth < 0)
            {
                yield break;
            }

            yield return 0;
            for (var i = 1; i <= depth; i++)
            {
                yield return i;
                yield return -i;
            }
        }
    }

    // TODO: have some way of making this hidden from inference
    public static class NonNeg
    {
        /// <summary>
        /// Serial instance for signed integers, where the depth is taken as the
        /// maximum absolute value.
        /// </summary>
        public instance SerialInt : CSerial<int>
        {
            IEnumerable<int> Series(int depth)
            {
                if (depth < 0)
                {
                    yield break;
                }

                yield return 0;
                for (var i = 1; i <= depth; i++)
                {
                    yield return i;
                }
            }
        }
    }

    /// <summary>
    /// Serial instance for arrays.
    /// </summary>
    public instance SerialArray<A, implicit SerialA> : CSerial<A[]>
        where SerialA : CSerial<A>
    {
        IEnumerable<A[]> Series(int depth)
        {
            if (depth < 0)
            {
                yield break;
            }

            yield return (new A[] { });

            foreach (var l in SerialA.Series(depth - 1))
            {
                foreach (var rs in this.Series(depth - 1))
                {
                    var len = rs.Length;
                    var xs = new A[len + 1];
                    Array.Copy(rs, 0, xs, 1, len);
                    xs[0] = l;
                    yield return xs;
                }
            }
        }
    }

    /// <summary>
    /// Serial instance for value pairs.
    /// </summary>
    public instance SerialVTup2<A, B, implicit SerialA, implicit SerialB> : CSerial<(A, B)>
        where SerialA : CSerial<A>
        where SerialB : CSerial<B>
    {
        IEnumerable<(A, B)> Series(int depth) => SerialHelpers.Prod(SerialA.Series, SerialB.Series, depth);
    }

    /// <summary>
    /// Serial instnace for ranges.
    /// </summary>
    public instance SerialRange<A, implicit SerialA> : CSerial<System.Concepts.Range<A>>
        where SerialA : CSerial<A>
    {
        // This is probably not quite right.
        IEnumerable<System.Concepts.Range<A>> Series(int depth)
        {
            foreach ((var s, var c) in SerialHelpers.Prod(SerialA.Series, NonNeg.SerialInt.Series, depth))
            {
                yield return new System.Concepts.Range<A> { start = s, count = c };
            }
        }
    }

    #endregion Common CSerial instances

    /// <summary>
    /// Useful combinators for creating CSerial instances.
    /// <para>
    /// These come almost directly from the SmallCheck paper.
    /// </para>
    /// </summary>
    static class SerialHelpers
    {
        /// <summary>
        /// Combines the outputs of two serial generators.
        /// </summary>
        /// <typeparam name="R">
        /// The serial type.
        /// </typeparam>
        /// <param name="lseries">
        /// The generator of the first series to combine.
        /// </param>
        /// <param name="rseries">
        /// The generator of the second series to combine.
        /// </param>
        /// <param name="depth">
        /// The depth for the combined generator.
        /// </param>
        /// <returns>
        /// All results from both series generators at the given depth.
        /// </returns>
        public static IEnumerable<R> Sum<R>(Func<int, IEnumerable<R>> lseries, Func<int, IEnumerable<R>> rseries, int depth) => lseries(depth).Concat(rseries(depth));

        /// <summary>
        /// Uses two serial generators to generate Cartesian products.
        /// </summary>
        /// <typeparam name="R">
        /// The serial type of the left-hand side of the product.
        /// </typeparam>
        /// <typeparam name="S">
        /// The serial type of the right-hand side of the product.
        /// </typeparam>
        /// <param name="lseries">
        /// The generator of the left-hand series.
        /// </param>
        /// <param name="rseries">
        /// The generator of the right-hand series.
        /// </param>
        /// <param name="depth">
        /// The depth for the combined generator.
        /// </param>
        /// <returns>
        /// All Cartesian products from both generators at the given depth.
        /// </returns>
        public static IEnumerable<(R, S)> Prod<R, S>(Func<int, IEnumerable<R>> lseries, Func<int, IEnumerable<S>> rseries, int depth) => from l in lseries(depth) from r in rseries(depth) select (l, r);

        #region Combinators for constructors

        // We don't provide the paper's Cons0, as it's trivial
        // (yield return).

        /// <summary>
        /// Adapts a 1-argument constructor of a sum type into a series
        /// generator.
        /// </summary>
        /// <typeparam name="A">
        /// The type of input into the constructor.
        /// </typeparam>
        /// <typeparam name="R">
        /// The type of output from the constructor.
        /// </typeparam>
        /// <typeparam name="SerialA">
        /// The witness for CSerial for the input type.
        /// </typeparam>
        /// <param name="f">
        /// The constructor.
        /// </param>
        /// <param name="depth">
        /// The depth at which to generate the series.
        /// </param>
        /// <returns>
        /// The generated series.
        /// </returns>
        public static IEnumerable<R> Cons1<A, R, implicit SerialA>(Func<A, R> f, int depth)
            where SerialA : CSerial<A>
        {
            if (depth > 0)
            {
                foreach (var a in SerialA.Series(depth - 1))
                {
                    yield return f(a);
                }
            }
        }

        /// <summary>
        /// Adapts a 2-argument constructor of a sum type into a series
        /// generator.
        /// </summary>
        /// <typeparam name="A">
        /// The first type of input into the constructor.
        /// </typeparam>
        /// <typeparam name="B">
        /// The second type of input into the constructor.
        /// </typeparam>
        /// <typeparam name="R">
        /// The type of output from the constructor.
        /// </typeparam>
        /// <typeparam name="SerialA">
        /// The witness for CSerial for the first input type.
        /// </typeparam>
        /// <typeparam name="SerialB">
        /// The witness for CSerial for the second input type.
        /// </typeparam>
        /// <param name="f">
        /// The constructor.
        /// </param>
        /// <param name="depth">
        /// The depth at which to generate the series.
        /// </param>
        /// <returns>
        /// The generated series.
        /// </returns>
        public static IEnumerable<R> Cons2<A, B, R, implicit SerialA, implicit SerialB>(Func<A, B, R> f, int depth)
            where SerialA : CSerial<A>
            where SerialB : CSerial<B>
        {
            if (depth > 0)
            {
                foreach (var (a, b) in Prod(SerialA.Series, SerialB.Series, depth - 1))
                {
                    yield return f(a, b);
                }
            }
        }

        #endregion Combinators for constructors
    }
}
