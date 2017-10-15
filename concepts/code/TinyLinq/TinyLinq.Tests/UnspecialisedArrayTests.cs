using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Prelude;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Showable;
using SerialPBT;

namespace TinyLinq
{
    public static class UnspecialisedArrayTests
    {
        /// <summary>
        /// Select and ToArray behaves as identity.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_UnspecSelectIdentityIntArray(int[] toSum)
        {
            var ts = toSum.Select(x => x).ToArray();
            if (ts.Length != toSum.Length)
            {
                return false;
            }
            for (var i = 0; i < ts.Length; i++)
            {
                if (ts[i] != toSum[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_UnspecSumEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum) == toSum.Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumSquaresEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Select<int, int>(x => x * x)) == toSum.Select(x => x * x).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd numbers of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumOddEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Where<int>(x => x % 2 == 1)) == toSum.Where(x => x % 2 == 1).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumSquaredOddEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Where<int>(x => x % 2 == 1).Select<int, int>(x => x * x)) ==
               toSum.Where(x => x % 2 == 1).Select(x => x * x).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumOddSquaresEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)) ==
               toSum.Select(x => x * x).Where(x => x % 2 == 1).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the length of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_UnspecCountEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount) == toCount.Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountSquaresEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Select<int, int>(x => x * x)) == toCount.Select(x => x * x).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd numbers of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountOddEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Where<int>(x => x % 2 == 1)) == toCount.Where(x => x % 2 == 1).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountSquaredOddEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Where<int>(x => x % 2 == 1).Select<int, int>(x => x * x)) ==
               toCount.Where(x => x % 2 == 1).Select(x => x * x).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountOddSquaresEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)) ==
               toCount.Select(x => x * x).Where(x => x % 2 == 1).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static Imp<bool, Func<bool>> Prop_UnspecAverageEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => Enumerable.Average(toAverage) == toAverage.Average()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all squares of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => Enumerable.Average(toAverage.Select<int, int>(x => x * x)) == toAverage.Select(x => x * x).Average()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd numbers of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageOddEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Where<int>(x => x % 2 == 1).Count(),
                (Func<bool>)(() => Enumerable.Average(toAverage.Where<int>(x => x % 2 == 1)) ==
                 toAverage.Where(x => x % 2 == 1).Average()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageSquaredOddEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < Enumerable.Count(toAverage.Where<int>(x => x % 2 == 1).Select<int, int>(x => x * x)),
                (Func<bool>)(() => Enumerable.Average(toAverage.Where<int>(x => x % 2 == 1).Select<int, int>(x => x * x)) ==
                 toAverage.Where(x => x % 2 == 1).Select(x => x * x).Average()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageOddSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < Enumerable.Count(toAverage.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)),
                (Func<bool>)(() => Enumerable.Average(toAverage.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)) ==
                 toAverage.Select(x => x * x).Where(x => x % 2 == 1).Average()));

        private static bool Prop_UnspecCartesianProductsEqualIntArray(int[] input)
        {
            var linq =
                Enumerable.ToArray(
                    input.SelectMany<int, int, (int, int)>(x => input,
                                                           (x, y) => (x, y)));
            var tiny =
                input.SelectMany(x => input,
                                 (x, y) => (x, y)).ToArray();
            if (linq.Length != tiny.Length)
            {
                return false;
            }

            for (var i = 0; i < linq.Length; i++)
            {
                if (linq[i].Item1 != tiny[i].x)
                {
                    return false;
                }
                if (linq[i].Item2 != tiny[i].y)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Prop_UnspecCartesianMultiplicationEqualIntArray(int[] xs, int[] ys)
            => Enumerable.Sum(xs.SelectMany<int, int, int>(x => ys, (x, y) => x * y)) ==
               xs.SelectMany(x => ys, (x, y) => x * y).Sum();

        private static Imp<bool, Func<bool>> Prop_Range_Sum(int start, int count)
            => PBTHelpers.Implies(
                0 < count,
                (Func<bool>) (() =>
                    {
                        var lcount = Enumerable.Sum(Enumerable.Range(start, count));
                        var rcount = new Range<int> { start = start, count = count }.Sum();
                        return lcount == rcount;
                    }
                ));

        private static Imp<bool, Func<bool>> Prop_Range_Count(int start, int count)
            => PBTHelpers.Implies(
                0 < count,
                (Func<bool>) (() =>
                    {
                        var lcount = Enumerable.Count(Enumerable.Range(start, count));
                        var rcount = new Range<int> { start = start, count = count }.Count();
                        return lcount == rcount;
                    }
                ));

        private static Imp<bool, Func<bool>> Prop_PythagoreanTriples_Sum(int max)
            => PBTHelpers.Implies(
                0 < max,
                (Func<bool>)(() =>
                {
                    var lsum =
                        Enumerable.Sum(
                            Enumerable.Select(
                                Enumerable.Where(
                                    Enumerable.SelectMany(
                                        Enumerable.SelectMany(
                                            Enumerable.Range(1, max + 1),
                                            a => Enumerable.Range(a, max + 1 - a),
                                            (a, b) => new { a, b }),
                                        z => Enumerable.Range(z.b, max + 1 - z.b),
                                        (z, c) => new { z, c }),
                                    y => y.z.a * y.z.a + y.z.b * y.z.b == y.c * y.c),
                                y => y.z.a + y.z.b + y.c));

                    var tlsum =
                        (from a in new Range<int> { start = 1, count = max + 1 }
                         from b in new Range<int> { start = a, count = max + 1 - a }
                         from c in new Range<int> { start = b, count = max + 1 - b }
                         where a * a + b * b == c * c
                         select a+b+c).Sum();
                    return lsum == tlsum;
                }
            ));

        private static Imp<bool, Func<bool>> Prop_PythagoreanTriples_Count(int max)
            => PBTHelpers.Implies(
                0 < max,
                (Func<bool>)(() =>
                {
                    var lcount =
                        Enumerable.Count(
                            Enumerable.Select(
                                Enumerable.Where(
                                    Enumerable.SelectMany(
                                        Enumerable.SelectMany(
                                            Enumerable.Range(1, max + 1),
                                            a => Enumerable.Range(a, max + 1 - a),
                                            (a, b) => new { a, b }),
                                        z => Enumerable.Range(z.b, max + 1 - z.b),
                                        (z, c) => new { z, c }),
                                    y => y.z.a * y.z.a + y.z.b * y.z.b == y.c * y.c),
                                y => true));
                    var tlcount =
                        (from a in new Range<int> { start = 1, count = max + 1 }
                         from b in new Range<int> { start = a, count = max + 1 - a }
                         from c in new Range<int> { start = b, count = max + 1 - b }
                         where a * a + b * b == c * c
                         select true).Count();
                    return lcount == tlcount;
                }
            ));

        public static void Run()
        {
            PBTHelpers.Check(Prop_UnspecSelectIdentityIntArray, 7);

            PBTHelpers.Check(Prop_UnspecSumEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecSumSquaresEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecSumOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecSumSquaredOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecSumOddSquaresEqualIntArray, 7);

            PBTHelpers.Check(Prop_UnspecAverageEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecCountSquaresEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecCountOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecCountSquaredOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecCountOddSquaresEqualIntArray, 7);

            PBTHelpers.Check(Prop_UnspecAverageEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecAverageSquaresEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecAverageOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecAverageSquaredOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecAverageOddSquaresEqualIntArray, 7);

            PBTHelpers.Check(Prop_UnspecCartesianProductsEqualIntArray, 7);
            PBTHelpers.Check(Prop_UnspecCartesianMultiplicationEqualIntArray, 5);

            // TODO: move elsewhere
            PBTHelpers.Check(Prop_Range_Sum, 20);
            PBTHelpers.Check(Prop_Range_Count, 20);

            PBTHelpers.Check(Prop_PythagoreanTriples_Sum, 20);
            PBTHelpers.Check(Prop_PythagoreanTriples_Count, 20);
        }
    }
}
