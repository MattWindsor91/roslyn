using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Prelude;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Showable;
using TinyLinq.SpecialisedInstances;
using SerialPBT;

namespace TinyLinq
{   
    public static class SpecialisedArrayTests
    {
        /// <summary>
        /// Select and ToArray behaves as identity.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_SelectIdentityIntArray(int[] toSum)
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
        private static bool Prop_SumEqualIntArray(int[] toSum)
            => toSum.Sum() == toSum.CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumSquaresEqualIntArray(int[] toSum)
            => toSum.Select(x => x * x).Sum() == toSum.CSelect((int x) => x * x).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd numbers of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumOddEqualIntArray(int[] toSum)
            => toSum.Where<int>(x => x % 2 == 1).Sum() == toSum.CWhere((int x) => x % 2 == 1).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumSquaredOddEqualIntArray(int[] toSum)
            => toSum.Where<int>(x => x % 2 == 1).Select(x => x * x).Sum() ==
               toSum.CWhere((int x) => x % 2 == 1).CSelect((int x) => x * x).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumOddSquaresEqualIntArray(int[] toSum)
            => toSum.Select(x => x * x).Where(x => x % 2 == 1).Sum() ==
               toSum.CSelect((int x) => x * x).CWhere((int x) => x % 2 == 1).CSum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the length of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_CountEqualIntArray(int[] toCount)
            => toCount.Count() == toCount.CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountSquaresEqualIntArray(int[] toCount)
            => toCount.Select(x => x * x).Count() == toCount.CSelect((int x) => x * x).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd numbers of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountOddEqualIntArray(int[] toCount)
            => toCount.Where<int>(x => x % 2 == 1).Count() == toCount.CWhere((int x) => x % 2 == 1).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountSquaredOddEqualIntArray(int[] toCount)
            => toCount.Where<int>(x => x % 2 == 1).Select(x => x * x).Count() ==
               toCount.CWhere((int x) => x % 2 == 1).CSelect((int x) => x * x).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountOddSquaresEqualIntArray(int[] toCount)
            => toCount.Select(x => x * x).Where(x => x % 2 == 1).Count() ==
               toCount.CSelect((int x) => x * x).CWhere((int x) => x % 2 == 1).CCount();

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static Imp<bool, Func<bool>> Prop_AverageEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => toAverage.Average() == toAverage.CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all squares of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_AverageSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => toAverage.Select(x => x * x).Average() == toAverage.CSelect((int x) => x * x).CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd numbers of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_AverageOddEqualIntArray(int[] toAverage)
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

        private static Imp<bool, Func<bool>> Prop_AverageSquaredOddEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Where<int>(x => x % 2 == 1).Select(x => x * x).Count(),
                (Func<bool>)(() => toAverage.Where<int>(x => x % 2 == 1).Select(x => x * x).Average() ==
                 toAverage.CWhere((int x) => x % 2 == 1).CSelect((int x) => x * x).CAverage()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_AverageOddSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Select(x => x * x).Where(x => x % 2 == 1).Count(),
                (Func<bool>)(() => toAverage.Select(x => x * x).Where(x => x % 2 == 1).Average() ==
                 toAverage.CSelect((int x) => x * x).CWhere((int x) => x % 2 == 1).CAverage()));

        private static bool Prop_CartesianProductsEqualIntArray(int[] input)
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

        private static bool Prop_CartesianMultiplicationEqualIntArray(int[] xs, int[] ys)
            => xs.SelectMany(x => ys, (x, y) => x * y).Sum() ==
               xs.CSelectMany((int x) => ys, (int x, int y) => x * y).CSum();

        public static void Run()
        {
            PBTHelpers.Check(Prop_SelectIdentityIntArray, 7);

            PBTHelpers.Check(Prop_SumEqualIntArray, 7);
            PBTHelpers.Check(Prop_SumSquaresEqualIntArray, 7);
            PBTHelpers.Check(Prop_SumOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_SumSquaredOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_SumOddSquaresEqualIntArray, 7);

            PBTHelpers.Check(Prop_AverageEqualIntArray, 7);
            PBTHelpers.Check(Prop_CountSquaresEqualIntArray, 7);
            PBTHelpers.Check(Prop_CountOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_CountSquaredOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_CountOddSquaresEqualIntArray, 7);

            PBTHelpers.Check(Prop_AverageEqualIntArray, 7);
            PBTHelpers.Check(Prop_AverageSquaresEqualIntArray, 7);
            PBTHelpers.Check(Prop_AverageOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_AverageSquaredOddEqualIntArray, 7);
            PBTHelpers.Check(Prop_AverageOddSquaresEqualIntArray, 7);

            PBTHelpers.Check(Prop_CartesianProductsEqualIntArray, 7);
            PBTHelpers.Check(Prop_CartesianMultiplicationEqualIntArray, 5);
        }
    }
}
