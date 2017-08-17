using System;
using System.Concepts;

// The CTestable concept for things that can be run as a test, and basic
// instances for setting up functions returning Bools (etc) as tests.
//
// For more exotic testable snap-ins, see Modifiers.

namespace SerialPBT
{
    /// <summary>
    /// Concept for types representing tests.
    /// </summary>
    /// <typeparam name="T">
    /// The type of tests.
    /// </typeparam>
    /// <typeparam name="InputTrace">
    /// The type of input traces, used to output information when a test fails.
    /// The type of input trace is dependent on the type of test.
    /// </typeparam>
    public concept CTestable<T, [AssociatedType] InputTrace>
    {
        /// <summary>
        /// Gets the name of a test.
        /// </summary>
        /// <param name="test">
        /// The test to be named.
        /// </param>
        /// <returns>
        /// The name of the test.
        /// </returns>
        string Name(T test) => "(untitled)";

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
        TestResult<InputTrace> Test(T test, int depth);
    }

    #region Common instances for CTestable

    /// <summary>
    /// Instance allowing Booleans to be testable.
    /// <para>
    /// This is usually the base case of a testable chain.  It needs no input
    /// generation, so returns an empty input trace.
    /// </para>
    /// </summary>
    public instance TestableBool : CTestable<bool, EndTrace>
    {
        string Name(bool _) => "(Boolean)";

        TestResult<EndTrace> Test(bool test, int depth) =>
            test ? TestResult<EndTrace>.Pass(new EndTrace()) : TestResult<EndTrace>.Fail(new EndTrace());
    }

    /// <summary>
    /// Instance allowing testing of arity-0 functions.
    /// <para>
    /// This allows lazy evaluation of tests, for instance.
    /// </para>
    /// </summary>
    public instance TestableF0<T, [AssociatedType] R, implicit TestableT> : CTestable<Func<T>, R>
        where TestableT : CTestable<T, R>
    {
        string Name(Func<T> test) => test.Method?.Name ?? "(unnamed func)";
        TestResult<R> Test(Func<T> f, int depth) => TestableT.Test(f(), depth);
    }

    /// <summary>
    /// Instance allowing testing of arity-1 functions.
    /// </summary>
    public instance TestableF1<A, T, [AssociatedType] R, implicit SerialA, implicit TestableT> : CTestable<Func<A, T>, F1Trace<A, R>>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        string Name(Func<A, T> test) => test.Method?.Name ?? "(unnamed func)";

        TestResult<F1Trace<A, R>> Test(Func<A, T> f, int depth)
        {
            var result = new TestResult<F1Trace<A, R>> {};
            foreach (var a in SerialA.Series(depth))
            {
                result.Merge(TestableT.Test(f(a), depth), (w) => new F1Trace<A, R> { input = a, next = w });
                if (result.Failed)
                {
                    break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Instance allowing testing of arity-2 functions.
    /// </summary>
    public instance TestableF2<A, B, T, [AssociatedType] R, implicit SerialA, implicit SerialB, implicit TestableT> : CTestable<Func<A, B, T>, F2Trace<A, B, R>>
        where SerialA : CSerial<A>
        where SerialB : CSerial<B>
        where TestableT : CTestable<T, R>
    {
        string Name(Func<A, B, T> test) => test.Method?.Name ?? "(unnamed func)";

        TestResult<F2Trace<A, B, R>> Test(Func<A, B, T> f, int depth)
        {
            var result = new TestResult<F2Trace<A, B, R>> { };
            foreach (var a in SerialA.Series(depth))
            {
                foreach (var b in SerialB.Series(depth))
                {
                    result.Merge(TestableT.Test(f(a, b), depth), (w) => new F2Trace<A, B, R> { input1 = a, input2 = b, next = w });
                    if (result.Failed)
                    {
                        break;
                    }
                }
            }
            return result;
        }
    }

    #endregion Common instances for CTestable
}
