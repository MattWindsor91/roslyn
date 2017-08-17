using System;
using System.Concepts;
using System.Linq;

/// <summary>
/// A small serial property-based testing library, mostly based on the paper
/// 'SmallCheck and LazySmallCheck: automatic exhaustive testing for small
/// values' by Runciman et al.
///
/// This example is based entirely on the paper with some tweaks and gap
/// filling.  It uses no code from any existing property-based testing
/// library, nor is it inspired by any.
/// </summary>
namespace SerialPBT
{
    class Program
    {
        static TestResult<R> Check<T, [AssociatedType] R, implicit TestableT>(T test, int depth)
            where TestableT : CTestable<T, R>
        {
            return TestableT.Test(test, depth);
        }

        static bool Prop_IsSingleDigit(int num) => -10 < num && num < 10;

        static Exists<int, bool> Prop_IsSingleDigit_Exists =>
            new Exists<int, bool> { property = Prop_IsSingleDigit };

        static Filtered<int, bool> Prop_IsSingleDigit_Filtered =>
            new Filtered<int, bool>
            {
                filter = (x) => x == 0,
                property = Prop_IsSingleDigit
            };

        /// <summary>
        /// Broken function that checks whether one array is a prefix of
        /// another.
        /// </summary>
        /// <param name="xs">The smaller array.</param>
        /// <param name="ys">The larger array.</param>
        /// <returns>
        /// In theory, true if and only if <paramref name="xs"/> is a
        /// prefix of <paramref name="ys"/>.
        /// In practice, there are deliberate bugs.
        /// </returns>
        static bool IsPrefixFaulty(int[] xs, int[] ys)
        {
            for (var i = 0; ; i++)
            {
                if (xs.Length <= i)
                {
                    return true;
                }
                if (ys.Length <= i)
                {
                    return false;
                }
                if (xs[i] == ys[i])
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Checks whether one array is a prefix of another.
        /// </summary>
        /// <param name="xs">The smaller array.</param>
        /// <param name="ys">The larger array.</param>
        /// <returns>
        /// True if and only if <paramref name="xs"/> is a prefix of
        /// <paramref name="ys"/>.
        /// </returns>
        static bool IsPrefixValid(int[] xs, int[] ys)
        {
            if (ys.Length < xs.Length)
            {
                return false;
            }
            return Enumerable.SequenceEqual(xs, Enumerable.Take(ys, xs.Length));
        }

        class IsPrefixTests<A>
        {
            Func<A[], A[], bool> isPrefix_Impl;

            public IsPrefixTests(Func<A[], A[], bool> impl)
            {
                isPrefix_Impl = impl;
            }
            public bool Prop_IsPrefix((A[] xs, A[] ys) tup)
            {
                var zs = new A[tup.xs.Length + tup.ys.Length];
                Array.Copy(tup.xs, zs, tup.xs.Length);
                Array.Copy(tup.ys, 0, zs, tup.xs.Length, tup.ys.Length);
                return isPrefix_Impl(tup.xs, zs);
            }

            public Filtered<(A[], A[]), Exists<A[], bool>> Prop_IsPrefix_Sound =>
                new Filtered<(A[], A[]), Exists<A[], bool>>
                {
                    filter = (t) => isPrefix_Impl(t.Item1, t.Item2),
                    property =
                        (t) =>
                            new Exists<A[], bool>
                            {
                                property = (rest) => Enumerable.SequenceEqual(t.Item1.Concat(rest), t.Item2)
                            }
                };
        }

        static void Main(string[] args)
        {
            for (var i = 0; i < 3; i++)
            {
                Console.WriteLine($"Arrays of size {i}");
                foreach (var ary in CSerial<int[]>.Series(i))
                {
                    ShowableHelpers.WriteLine(ary);
                }
            }

            ShowableHelpers.WriteLine(Check<Func<int, bool>>(Prop_IsSingleDigit, 9));
            Console.WriteLine("---");
            ShowableHelpers.WriteLine(Check<Func<int, bool>>(Prop_IsSingleDigit, 11));
            Console.WriteLine("---");
            ShowableHelpers.WriteLine(Check(Prop_IsSingleDigit_Exists, 11));
            Console.WriteLine("---");
            ShowableHelpers.WriteLine(Check(Prop_IsSingleDigit_Filtered, 11));
            Console.WriteLine("---");

            var faulty = new IsPrefixTests<int>(IsPrefixFaulty);

            ShowableHelpers.WriteLine(Check<Func<(int[], int[]), bool>>(faulty.Prop_IsPrefix, 4));
            Console.WriteLine("---");
            ShowableHelpers.WriteLine(Check(faulty.Prop_IsPrefix_Sound, 4));
            Console.WriteLine("---");

            var valid = new IsPrefixTests<int>(IsPrefixValid);

            ShowableHelpers.WriteLine(Check<Func<(int[], int[]), bool>>(valid.Prop_IsPrefix, 4));
            Console.WriteLine("---");
            ShowableHelpers.WriteLine(Check(valid.Prop_IsPrefix_Sound, 4));
            Console.WriteLine("---");
        }
    }
}
