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
            var ts = toSum.CSelect((int x) => x).CToArray();
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
            => toSum.Sum() == toSum.CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumSquaresEqualIntArray(int[] toSum)
            => toSum.Select<int, int>(x => x * x).Sum() == toSum.CSelect((int x) => x * x).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd numbers of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumOddEqualIntArray(int[] toSum)
            => toSum.Where<int>(x => x % 2 == 1).Sum() == toSum.CWhere((int x) => x % 2 == 1).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumSquaredOddEqualIntArray(int[] toSum)
            => toSum.Where<int>(x => x % 2 == 1).Select(x => x * x).Sum() ==
               toSum.Where(x => x % 2 == 1).Select((int x) => x * x).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecSumOddSquaresEqualIntArray(int[] toSum)
            => toSum.Select<int, int>(x => x * x).Where(x => x % 2 == 1).Sum() ==
               toSum.Select((int x) => x * x).Where((int x) => x % 2 == 1).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the length of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_UnspecCountEqualIntArray(int[] toCount)
            => toCount.Count() == toCount.CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountSquaresEqualIntArray(int[] toCount)
            => toCount.Select<int, int>(x => x * x).Count() == toCount.CSelect((int x) => x * x).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd numbers of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountOddEqualIntArray(int[] toCount)
            => toCount.Where<int>(x => x % 2 == 1).Count() == toCount.CWhere((int x) => x % 2 == 1).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountSquaredOddEqualIntArray(int[] toCount)
            => toCount.Where<int>(x => x % 2 == 1).Select(x => x * x).Count() ==
               toCount.Where((int x) => x % 2 == 1).CSelect((int x) => x * x).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_UnspecCountOddSquaresEqualIntArray(int[] toCount)
            => toCount.Select<int, int>(x => x * x).Where(x => x % 2 == 1).Count() ==
               toCount.Select(x => x * x).Where((int x) => x % 2 == 1).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static Imp<bool, Func<bool>> Prop_UnspecAverageEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => toAverage.Average() == toAverage.CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all squares of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => toAverage.Select<int, int>(x => x * x).Average() == toAverage.Select(x => x * x).CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd numbers of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageOddEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Where<int>(x => x % 2 == 1).Count(),
                (Func<bool>)(() => toAverage.Where<int>(x => x % 2 == 1).Average() ==
                 toAverage.CWhere((int x) => x % 2 == 1).CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageSquaredOddEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Where<int>(x => x % 2 == 1).Select(x => x * x).Count(),
                (Func<bool>)(() => toAverage.Where<int>(x => x % 2 == 1).Select(x => x * x).Average() ==
                 toAverage.Where((int x) => x % 2 == 1).Select((int x) => x * x).CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_UnspecAverageOddSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Select<int, int>(x => x * x).Where(x => x % 2 == 1).Count(),
                (Func<bool>)(() => toAverage.Select<int, int>(x => x * x).Where(x => x % 2 == 1).Average() ==
                 toAverage.Select(x => x * x).Where((int x) => x % 2 == 1).CAverage()));

        private static bool Prop_UnspecCartesianProductsEqualIntArray(int[] input)
        {
            var linq =
                input.SelectMany(x => input,
                                 (x, y) => (x, y)).ToArray();
            var tiny =
                input.CSelectMany((int x) => input,
                                  (int x, int y) => (x, y)).CToArray();
            if (linq.Length != tiny.Length)
            {
                return false;
            }

            for (var i = 0; i < linq.Length; i++)
            {
                if (linq[i].Item1 != tiny[i].Item1)
                {
                    return false;
                }
                if (linq[i].Item2 != tiny[i].Item2)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Prop_UnspecCartesianMultiplicationEqualIntArray(int[] xs, int[] ys)
            => xs.SelectMany(x => ys, (x, y) => x * y).Sum() ==
               xs.CSelectMany((int x) => ys, (int x, int y) => x * y).CSum();

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
        }
    }
}
