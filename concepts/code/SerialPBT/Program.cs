using System;
using System.Collections.Generic;
using System.Concepts;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

    public class TestResult<R>
    {
        private bool success = true;
        private R witnessOpt = default;
        private Queue<R> skipped = new Queue<R>();
        private int testNum = 0;

        public bool Succeeded => success;
        public int LastTestNum => testNum;
        public R Witness => witnessOpt;
        public IEnumerable<R> Skipped => skipped;

        public void IncTestNum(int howMuch)
        {
            testNum += howMuch;
        }

        public void Fail(R failure)
        {
            success = false;
            witnessOpt = failure;
        }

        public void Pass(R witness)
        {
            success = true;
            witnessOpt = witness;
        }

        public void Skip(R toSkip)
        {
            skipped.Enqueue(toSkip);
        }

        public void Skip(IEnumerable<R> toSkips)
        {
            foreach (var toSkip in toSkips)
            {
                Skip(toSkip);
            }
        }
    }

    public instance ShowableTestResult<R, implicit ShowableR> : CShowable<TestResult<R>>
        where ShowableR : CShowable<R>
    {
        public void Show(TestResult<R> me, StringBuilder sb)
        {
            if (me.Succeeded)
            {
                sb.Append("Passed after ");
                CShowable<int>.Show(me.LastTestNum, sb);
                sb.AppendLine(" tests.");
            }
            else
            {
                sb.Append("Failed at test ");
                CShowable<int>.Show(me.LastTestNum, sb);
                sb.AppendLine(":");
                sb.Append("  ");
                ShowableR.Show(me.Witness, sb);
                sb.AppendLine();
            }

            var sc = me.Skipped.Count();
            if (0 < sc)
            {
                sb.Append("  Skipped ");
                CShowable<int>.Show(sc, sb);
                sb.AppendLine(" tests, for example:");

                int i = 0;
                foreach (var skipped in me.Skipped.Take(10))
                {
                    i++;
                    sb.Append("  ");
                    CShowable<int>.Show(i, sb);
                    sb.Append(". ");
                    ShowableR.Show(skipped, sb);
                    sb.AppendLine();
                }
            }
        }
    }

    struct Existential<A, T>
    {
        public Func<A, T> property;
    }

    instance TestableExistential<A, T, [AssociatedType] R, implicit ShowableA, implicit SerialA, implicit TestableT> : CTestable<Existential<A, T>, bool>
        where ShowableA : CShowable<A>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        TestResult<bool> Test(Existential<A, T> f, int depth)
        {
            var result = new TestResult<bool>();

            foreach (var a in SerialA.Series(depth))
            {
                var innerResult = TestableT.Test(f.property(a), depth);
                result.IncTestNum(innerResult.LastTestNum);

                if (innerResult.Succeeded)
                {
                    return result;
                }
                result.Skip(from r in innerResult.Skipped select true);
            }

            result.Fail(false);
            return result;
        }
    }

    public struct Filtered<A, T>
    {
        public Func<A, bool> filter;
        public Func<A, T> property;
    }

    public instance TestableFiltered<A, T, [AssociatedType] R, implicit SerialA, implicit TestableT> : CTestable<Filtered<A, T>, (A, R)>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        TestResult<(A, R)> Test(Filtered<A, T> f, int depth)
        {
            var result = new TestResult<(A, R)>();

            foreach (var a in SerialA.Series(depth))
            {
                if (!f.filter(a))
                {
                    result.Skip((a, default));
                    continue;
                }

                var innerResult = TestableT.Test(f.property(a), depth);
                result.IncTestNum(innerResult.LastTestNum);

                if (!innerResult.Succeeded)
                {
                    result.Fail((a, innerResult.Witness));
                    return result;
                }
                result.Pass((a, innerResult.Witness));
                result.Skip(from r in innerResult.Skipped select (a, r));
            }

            return result;
        }
    }

    static class Helpers
    {
        public static IEnumerable<R> Sum<R>(Func<int, IEnumerable<R>> lseries, Func<int, IEnumerable<R>> rseries, int depth) => lseries(depth).Concat(rseries(depth));

        public static IEnumerable<(R, S)> Prod<R, S>(Func<int, IEnumerable<R>> lseries, Func<int, IEnumerable<S>> rseries, int depth) => from l in lseries(depth) from r in rseries(depth) select (l, r);

        public static IEnumerable<R> Cons0<R>(R f, int depth)
        {
            yield return f;
        }

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
    }

    /// <summary>
    /// Concept for types representing tests.
    /// </summary>
    /// <typeparam name="T">
    /// The type of tests.
    /// </typeparam>
    /// <typeparam name="R">
    /// The type of results returned when the test fails.
    /// </typeparam>
    public concept CTestable<T, [AssociatedType] R>
    {
        /// <summary>
        /// Tests a testable value.
        /// </summary>
        /// <param name="test">
        /// The value to test.
        /// </param>
        /// <param name="depth">
        /// The depth at which we are testing.
        /// </param>
        /// <param name="firstTestNum">
        /// The number of the first test in the series.
        /// </param>
        /// <returns>
        /// The result of the tests.
        /// </returns>
        TestResult<R> Test(T test, int depth);
    }

    /// <summary>
    /// Instance allowing Booleans to be testable.
    /// </summary>
    public instance TestableBool : CTestable<bool, bool>
    {
        TestResult<bool> Test(bool test, int depth)
        {
            var result = new TestResult<bool>();

            if (!test)
            {
                result.Fail(test);
            }

            result.IncTestNum(1);
            return result;
        }
    }

    public struct F1Trace<A, R>
    {
        public A input;
        public R next;
    }

    public instance ShowableF1Trace<A, R, implicit ShowableA, implicit ShowableR> : CShowable<F1Trace<A, R>>
        where ShowableA : CShowable<A>
        where ShowableR : CShowable<R>
    {
        void Show(F1Trace<A, R> trace, StringBuilder sb)
        {
            ShowableA.Show(trace.input, sb);
            sb.Append(" -> ");
            ShowableR.Show(trace.next, sb);
        }
    }

    /// <summary>
    /// Instance allowing arity-1 functions to be testable.
    /// </summary>
    public instance TestableF1<A, T, [AssociatedType] R, implicit SerialA, implicit TestableT> : CTestable<Func<A, T>, F1Trace<A, R>>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        TestResult<F1Trace<A, R>> Test(Func<A, T> f, int depth)
        {
            var result = new TestResult<F1Trace<A, R>>();

            foreach (var a in SerialA.Series(depth))
            {
                var innerResult = TestableT.Test(f(a), depth);
                result.IncTestNum(innerResult.LastTestNum);

                if (!innerResult.Succeeded)
                {
                    result.Fail(new F1Trace<A, R> { input = a, next = innerResult.Witness });
                    return result;
                }
                result.Pass(new F1Trace<A, R> { input = a, next = innerResult.Witness });
                result.Skip(from r in innerResult.Skipped select new F1Trace<A, R> { input = a, next = r });
            }

            return result;
        }
    }

    class Program
    {
        static TestResult<R> Check<T, [AssociatedType] R, implicit TestableT>(T test, int depth)
            where TestableT : CTestable<T, R>
        {
            return TestableT.Test(test, depth);
        }

        static bool Prop_IsSingleDigit(int num) => -10 < num && num < 10;

        static Existential<int, bool> Prop_IsSingleDigit_Exists =>
            new Existential<int, bool> { property = Prop_IsSingleDigit };

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

            public Filtered<(A[], A[]), Existential<A[], bool>> Prop_IsPrefix_Sound =>
                new Filtered<(A[], A[]), Existential<A[], bool>>
                {
                    filter = (t) => isPrefix_Impl(t.Item1, t.Item2),
                    property =
                        (t) =>
                            new Existential<A[], bool>
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
                    ShowableHelpers.Write(ary);
                }
            }

            ShowableHelpers.Write(Check<Func<int, bool>>(Prop_IsSingleDigit, 9));
            Console.WriteLine("---");
            ShowableHelpers.Write(Check<Func<int, bool>>(Prop_IsSingleDigit, 11));
            Console.WriteLine("---");
            ShowableHelpers.Write(Check(Prop_IsSingleDigit_Exists, 11));
            Console.WriteLine("---");
            ShowableHelpers.Write(Check(Prop_IsSingleDigit_Filtered, 11));
            Console.WriteLine("---");

            var faulty = new IsPrefixTests<int>(IsPrefixFaulty);

            ShowableHelpers.Write(Check<Func<(int[], int[]), bool>>(faulty.Prop_IsPrefix, 4));
            Console.WriteLine("---");
            ShowableHelpers.Write(Check(faulty.Prop_IsPrefix_Sound, 4));
            Console.WriteLine("---");

            var valid = new IsPrefixTests<int>(IsPrefixValid);

            ShowableHelpers.Write(Check<Func<(int[], int[]), bool>>(valid.Prop_IsPrefix, 4));
            Console.WriteLine("---");
            ShowableHelpers.Write(Check(valid.Prop_IsPrefix_Sound, 4));
            Console.WriteLine("---");
        }
    }
}
