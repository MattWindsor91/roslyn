using System;
using System.Concepts;

// Special structs that can be slotted into a test to change its behaviour.

namespace SerialPBT
{
    /// <summary>
    /// Renames a test.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the renamed test.
    /// </typeparam>
    public struct Named<T>
    {
        /// <summary>
        /// The name of the test.
        /// </summary>
        public string name;

        /// <summary>
        /// The renamed test.
        /// </summary>
        public T test;

        /// <summary>
        /// Constructs a new named test.
        /// </summary>
        /// <param name="n">
        /// The name of the test.
        /// </param>
        /// <param name="t">
        /// The test to name.
        /// </param>
        public Named(string n, T t)
        {
            name = n;
            test = t;
        }
    }

    /// <summary>
    /// Instance allowing testing of named tests.
    /// </summary>
    public instance TestableNamed<T, [AssociatedType] R, implicit TestableT> : CTestable<Named<T>, R>
        where TestableT : CTestable<T, R>
    {
        string Name(Named<T> test) => test.name;
        TestResult<R> Test(Named<T> f, int depth) => TestableT.Test(f.test, depth);
    }

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

        /// <summary>
        /// Creates an existential quantification.
        /// </summary>
        /// <param name="p">
        /// The property over which we are existentially quantifying.
        /// </param>
        public Exists(Func<A, T> p) { property = p; }
    }

    /// <summary>
    /// Instance allowing testing of existentials.
    /// </summary>
    instance TestableExists<A, T, [AssociatedType] R, implicit ShowableA, implicit SerialA, implicit TestableT> : CTestable<Exists<A, T>, ExistsTrace>
        where ShowableA : CShowable<A>
        where SerialA : CSerial<A>
        where TestableT : CTestable<T, R>
    {
        string Name(Exists<A, T> e)
        {
            var atype = e.property.GetType().GenericTypeArguments[0].ToString();
            return $"<exists some {atype} satisfying {e.property.Method?.Name ?? "(untitled)"}>";
        }

        TestResult<ExistsTrace> Test(Exists<A, T> f, int depth)
        {
            var result = new TestResult<ExistsTrace>();
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
    public struct Imp<TL, TR>
    {
        /// <summary>
        /// The filter to apply to candidate inputs.
        /// </summary>
        public TL filter;

        /// <summary>
        /// The property that must hold for all inputs that pass the filter.
        /// </summary>
        public TR property;
    }

    /// <summary>
    /// Instance allowing testing of implications.
    /// </summary>
    public instance TestableImp<TL, TR, [AssociatedType] RL, [AssociatedType] RR, implicit TestableL, implicit TestableR> : CTestable<Imp<TL, TR>, ImpTrace<RR>>
        where TestableL : CTestable<TL, RL>
        where TestableR : CTestable<TR, RR>
    {
        TestResult<ImpTrace<RR>> Test(Imp<TL, TR> f, int depth)
        {
            if (TestableL.Test(f.filter, depth).Failed)
            {
                return TestResult<ImpTrace<RR>>.Skip(new ImpTrace<RR> { skipped = true, next = default });
            }

            var result = new TestResult<ImpTrace<RR>>();
            result.Merge(TestableR.Test(f.property, depth), (r) => new ImpTrace<RR> { skipped = false, next = r });
            return result;
        }
    }
}
