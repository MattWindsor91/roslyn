using System;
using System.Collections.Generic;
using System.Linq;
using System.Concepts;
using System.Concepts.Prelude;
using System.Concepts.Enumerable;
using System.Concepts.Countable;
using System.Concepts.Indexable;
using static System.Concepts.Enumerable.Instances;
using System.Concepts.Showable;
using TinyLinq.SpecialisedInstances;
using SerialPBT;

namespace TinyLinq
{
    /// <summary>
    /// Tests that perform queries over arrays and use specialised
    /// TinyLINQ instances.
    /// </summary>
    public static class GenericTests
    {
        /// <summary>
        /// Select and ToArray behaves as identity.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        public static bool Prop_SelectIdentity<TColl, [AssociatedType]TElem, [AssociatedType]TSelectDst, implicit C, implicit S, implicit A, implicit I, implicit Q>(TColl toSum)
            where C : CCountable<TColl>
            where S : CSelect<TColl, TElem, TElem, TSelectDst>
            where A : CToArray<TSelectDst, TElem>
            where I : CIndexable<TColl, int, TElem>
            where Q : Eq<TElem>
        {
            var ts = S.Select(toSum, x => x).ToArray();
            if (ts.Length != toSum.Count())
            {
                return false;
            }
            for (var i = 0; i < ts.Length; i++)
            {
                if (ts[i] != toSum.At(i))
                {
                    return false;
                }
            }
            return true;
        }
        /*
        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_SumEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum) == toSum.Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumSquaresEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Select<int, int>(x => x * x)) == toSum.Select(x => x * x).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd numbers of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumOddEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Where<int>(x => x % 2 == 1)) == toSum.Where(x => x % 2 == 1).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumSquaredOddEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Where<int>(x => x % 2 == 1).Select<int, int>(x => x * x)) ==
               toSum.Where(x => x % 2 == 1).Select(x => x * x).Sum();


        /// <summary>
        /// LINQ and TinyLINQ agree on the sum of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toSum">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_SumOddSquaresEqualIntArray(int[] toSum)
            => Enumerable.Sum(toSum.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)) ==
               toSum.Select(x => x * x).Where(x => x % 2 == 1).Sum();

        /// <summary>
        /// LINQ and TinyLINQ agree on the length of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static bool Prop_CountEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount) == toCount.Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountSquaresEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Select<int, int>(x => x * x)) == toCount.Select(x => x * x).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd numbers of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountOddEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Where<int>(x => x % 2 == 1)) == toCount.Where(x => x % 2 == 1).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all squares of odd numbers
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountSquaredOddEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Where<int>(x => x % 2 == 1).Select<int, int>(x => x * x)) ==
               toCount.Where(x => x % 2 == 1).Select(x => x * x).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the count of all odd squares
        /// of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static bool Prop_CountOddSquaresEqualIntArray(int[] toCount)
            => Enumerable.Count(toCount.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)) ==
               toCount.Select(x => x * x).Where(x => x % 2 == 1).Count();

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of an array.
        /// </summary>
        /// <param name="toCount">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>
        private static Imp<bool, Func<bool>> Prop_AverageEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => Enumerable.Average(toAverage) == toAverage.Average()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all squares of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_AverageSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < toAverage.Length,
                (Func<bool>)(() => Enumerable.Average(toAverage.Select<int, int>(x => x * x)) == toAverage.Select(x => x * x).Average()));

        /// <summary>
        /// LINQ and TinyLINQ agree on the average of all odd numbers of an array.
        /// </summary>
        /// <param name="toAverage">The array to test against.</param>
        /// <returns>Whether LINQ and TinyLINQ agree.</returns>

        private static Imp<bool, Func<bool>> Prop_AverageOddEqualIntArray(int[] toAverage)
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

        private static Imp<bool, Func<bool>> Prop_AverageSquaredOddEqualIntArray(int[] toAverage)
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

        private static Imp<bool, Func<bool>> Prop_AverageOddSquaresEqualIntArray(int[] toAverage)
            => PBTHelpers.Implies(
                0 < Enumerable.Count(toAverage.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)),
                (Func<bool>)(() => Enumerable.Average(toAverage.Select<int, int>(x => x * x).Where<int>(x => x % 2 == 1)) ==
                 toAverage.Select(x => x * x).Where(x => x % 2 == 1).Average()));

        private static bool Prop_CartesianProductsEqualIntArray(int[] input)
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

        private static bool Prop_CartesianMultiplicationEqualIntArray(int[] xs, int[] ys)
            => Enumerable.Sum(xs.SelectMany<int, int, int>(x => ys, (x, y) => x * y)) ==
               xs.SelectMany(x => ys, (x, y) => x * y).Sum();

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
        */
    }
}
