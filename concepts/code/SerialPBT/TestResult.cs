using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SerialPBT
{
    /// <summary>
    /// A test result, containing a witness for the test, whether the witness
    /// is passing or failing, the set of skipped inputs, and the total
    /// number of seen tests.
    /// </summary>
    /// <typeparam name="R">
    /// The type of witnesses, which are usually some trace containing a series
    /// of inputs and the final test output.
    /// </typeparam>
    public class TestResult<R>
    {
        /// <summary>
        /// Possible outcomes for the test result.
        /// </summary>
        private enum Outcome
        {
            Passed,
            Failed,
            Inconclusive
        }

        /// <summary>
        /// The current outcome of the test.
        /// </summary>
        private Outcome outcome = Outcome.Inconclusive;

        /// <summary>
        /// The current witness input, if any.
        /// </summary>
        private R witnessOpt = default;

        /// <summary>
        /// The list of skipped inputs in this test.
        /// </summary>
        private Queue<R> skipped = new Queue<R>();

        /// <summary>
        /// The number of inputs this test result has consumed, not
        /// including skipped inputs.
        /// </summary>
        private int testNum = 0;

        /// <summary>
        /// Gets whether this test has passed.
        /// </summary>
        public bool Succeeded => outcome == Outcome.Passed;

        /// <summary>
        /// Gets whether this test has failed.
        /// </summary>
        public bool Failed => outcome == Outcome.Failed;

        /// <summary>
        /// Gets the number of non-skipped test cases logged in this test
        /// result.
        /// </summary>
        public int TestCount => testNum;

        /// <summary>
        /// Gets a witness for this test's success or failure.
        /// </summary>
        public R Witness
        {
            get
            {
                if (outcome == Outcome.Inconclusive)
                {
                    throw new InvalidOperationException("test must have passed or failed");
                }
                return witnessOpt;
            }
        }

        /// <summary>
        /// Gets the set of skipped test cases.
        /// </summary>
        public IEnumerable<R> Skipped => skipped;

        /// <summary>
        /// Merges an inner test result into this one, adding the set of
        /// skipped examples and total number of tests and replacing this
        /// result's outcome and witness with that of the inner.
        /// </summary>
        /// <typeparam name="R2">
        /// The type of witness traces in the inner test result.
        /// </typeparam>
        /// <param name="inner">
        /// The inner test result to merge.
        /// </param>
        /// <param name="witnessMapper">
        /// A mapping from inner witnesses to outer witnesses.
        /// </param>
        public void Merge<R2>(TestResult<R2> inner, Func<R2, R> witnessMapper)
        {
            testNum += inner.TestCount;

            var o = (Outcome)inner.outcome;
            if (o != Outcome.Inconclusive)
            {
                // NB: If we merge a failing test result onto a passing result,
                // or vice versa, the new outcome overrides the old one.
                // This is ok, as long as we handle the semantics properly in
                // the parent (eg. break at first failure for forall).
                outcome = o;
                witnessOpt = witnessMapper(inner.Witness);
            }

            foreach (var r in inner.Skipped)
            {
                skipped.Enqueue(witnessMapper(r));
            }
        }

        /// <summary>
        /// Reports a passing test case with the given witness.
        /// </summary>
        /// <param name="witness">
        /// The trace of the witness that generated a pass.
        /// </param>
        /// <returns>
        /// A test result representing the pass.
        /// </returns>
        public static TestResult<R> Pass(R witness) =>
            new TestResult<R>
            {
                outcome = Outcome.Passed,
                witnessOpt = witness,
                testNum = 1
            };

        /// <summary>
        /// Reports a failing test case with the given witness.
        /// </summary>
        /// <param name="witness">
        /// The trace of the witness that generated a failure.
        /// </param>
        /// <returns>
        /// A test result representing the failure.
        /// </returns>
        public static TestResult<R> Fail(R witness) =>
            new TestResult<R>
            {
                outcome = Outcome.Failed,
                witnessOpt = witness,
                testNum = 1
            };

        /// <summary>
        /// Reports a skipped test case.
        /// </summary>
        /// <param name="toSkip">
        /// The input to skip.
        /// </param>
        public static TestResult<R> Skip(R toSkip)
        {
            var res = new TestResult<R>
            {
                outcome = Outcome.Inconclusive
            };
            // Don't record this in testNum.
            res.skipped.Enqueue(toSkip);
            return res;
        }
    }

    /// <summary>
    /// Showable instance for test results, when their inputs are themselves
    /// showable.
    /// </summary>
    public instance ShowableTestResult<R, implicit ShowableR> : CShowable<TestResult<R>>
        where ShowableR : CShowable<R>
    {
        public void Show(TestResult<R> me, StringBuilder sb)
        {
            if (me.Succeeded)
            {
                sb.Append("Passed after ");
                CShowable<int>.Show(me.TestCount, sb);
                sb.AppendLine(" tests.");
            }
            else if (me.Failed)
            {
                sb.Append("Failed at test ");
                CShowable<int>.Show(me.TestCount, sb);
                sb.AppendLine(":");
                sb.Append("  ");
                ShowableR.Show(me.Witness, sb);
                sb.AppendLine();
            }
            else
            {
                sb.Append("Test inconclusive.");
            }

            var sc = me.Skipped.Count();
            if (0 < sc)
            {
                sb.Append("Skipped ");
                CShowable<int>.Show(sc, sb);
                sb.AppendLine(" tests, for example:");

                foreach (var skipped in me.Skipped.Take(10))
                {
                    sb.Append("  - ");
                    ShowableR.Show(skipped, sb);
                    sb.AppendLine();
                }
            }
        }
    }

}
