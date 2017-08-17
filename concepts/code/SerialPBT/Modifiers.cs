using System;
using System.Concepts;

// Special structs that can be slotted into a test to change its behaviour.

namespace SerialPBT
{

    /// <summary>
    /// Passes if, and only if, there exists an input that satisfies the
    /// given property.
    /// </summary>
    /// <typeparam name="A">
    /// Type of inputs over which we are existentially quantifying.
    /// </typeparam>
    /// <typeparam name="T">
    /// Type of the property at least one input must satisfy.
    /// </typeparam>
    struct Exists<A, T>
    {
        /// <summary>
        /// The property over which we are existentially quantifying.
        /// </summary>
        public Func<A, T> property;
    }

    /// <summary>
    /// Testable instance for existentials.
    /// </summary>
    instance TestableExists<A, T, [AssociatedType] R, implicit ShowableA, implicit SerialA, implicit TestableT> : CTestable<Exists<A, T>, ExistsTrace>
        where ShowableA : CShowable<A>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        TestResult<ExistsTrace> Test(Exists<A, T> f, int depth)
        {
            var result = new TestResult<ExistsTrace>();
            var atype = f.property.GetType().GenericTypeArguments[0].ToString();
            result.Name = $"<exists some {atype} satisfying {f.property.Method?.Name ?? "(untitled)"}>";

            foreach (var a in SerialA.Series(depth))
            {
                var innerResult = TestableT.Test(f.property(a), depth);
                result.Merge(innerResult, (r) => new ExistsTrace());
                if (result.Succeeded)
                {
                    break;
                }
            }
            return result;
        }
    }

    /// <summary>
    /// Applies a filter to a 1-arity forall property, skipping all inputs
    /// that fail the filter.
    /// </summary>
    /// <typeparam name="A">
    /// Type of inputs to the property.
    /// </typeparam>
    /// <typeparam name="T">
    /// Type of the property that must hold for all inputs that pass the
    /// filter.
    /// </typeparam>
    public struct Filtered<A, T>
    {
        /// <summary>
        /// The filter to apply to candidate inputs.
        /// </summary>
        public Func<A, bool> filter;
        /// <summary>
        /// The property that must hold for all inputs that pass the filter.
        /// </summary>
        public Func<A, T> property;
    }

    /// <summary>
    /// Instance allowing testing of filtered arity-1 functions.
    /// </summary>
    public instance TestableF1_Filtered<A, T, [AssociatedType] R, implicit SerialA, implicit TestableT> : CTestable<Filtered<A, T>, F1Trace<A, R>>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        TestResult<F1Trace<A, R>> Test(Filtered<A, T> f, int depth)
        {
            var result = new TestResult<F1Trace<A, R>>();
            result.Name = $"<filtered: {f.property.Method?.Name ?? "(untitled)"}>";

            foreach (var a in SerialA.Series(depth))
            {
                if (!f.filter(a))
                {
                    result.Skip(new F1Trace<A, R> { input = a, next = default, skipped = true });
                    continue;
                }

                result.Merge(TestableT.Test(f.property(a), depth), (r) => new F1Trace<A, R> { input = a, next = r, skipped = false });
                if (result.Failed)
                {
                    break;
                }
            }

            return result;
        }
    }
}
