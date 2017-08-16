using System;
using System.Collections.Generic;

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

            foreach ((var l, var rs) in Helpers.Prod(SerialA.Series, this.Series, depth - 1))
            {
                var len = rs.Length;
                var xs = new A[len + 1];
                Array.Copy(rs, 0, xs, 1, len);
                xs[0] = l;
                yield return xs;
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
        IEnumerable<(A, B)> Series(int depth) => Helpers.Prod(SerialA.Series, SerialB.Series, depth);
    }
}
