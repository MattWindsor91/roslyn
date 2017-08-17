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
        TestResult<EndTrace> Test(bool test, int depth)
        {
            return test ? TestResult<EndTrace>.Pass(new EndTrace()) : TestResult<EndTrace>.Fail(new EndTrace());
        }
    }

    /// <summary>
    /// Instance allowing testing of arity-0 functions.
    /// <para>
    /// This allows naming otherwise anonymous tests.
    /// </para>
    /// </summary>
    public instance TestableF0<T, [AssociatedType] R, implicit TestableT> : CTestable<Func<T>, R>
        where TestableT : CTestable<T, R>
    {
        TestResult<R> Test(Func<T> f, int depth)
        {
            var result = TestableT.Test(f(), depth);
            result.Name = f.Method?.Name;
            return result;
        }
    }

    /// <summary>
    /// Instance allowing testing of unfiltered arity-1 functions.
    /// </summary>
    public instance TestableF1<A, T, [AssociatedType] R, implicit SerialA, implicit TestableT> : CTestable<Func<A, T>, F1Trace<A, R>>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        TestResult<F1Trace<A, R>> Test(Func<A, T> f, int depth)
        {
            var result = new TestResult<F1Trace<A, R>>();
            result.Name = f.Method?.Name;

            foreach (var a in SerialA.Series(depth))
            {
                result.Merge(TestableT.Test(f(a), depth), (w) => new F1Trace<A, R> { input = a, next = w, skipped = false });
                if (result.Failed)
                {
                    break;
                }
            }

            return result;
        }
    }

    #endregion Common instances for CTestable
}
