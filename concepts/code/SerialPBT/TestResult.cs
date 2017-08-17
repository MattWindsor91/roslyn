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
        /// The name of the test.
        /// </summary>
        private string name = null;

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
        /// The number of inputs this test result has consumed.
        /// </summary>
        private int testNum = 0;

        /// <summary>
        /// Gets the name of this test.
        /// </summary>
        public string Name
        {
            get => name ?? "(untitled)";
            set => name = value;
        }

        /// <summary>
        /// Gets whether this test has passed.
        /// </summary>
        public bool Succeeded => outcome == Outcome.Passed;

        /// <summary>
        /// Gets whether this test has failed.
        /// </summary>
        public bool Failed => outcome == Outcome.Failed;

        /// <summary>
        /// Gets the number of test cases logged in this test result.
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
            outcome = (Outcome)inner.outcome;
            witnessOpt = witnessMapper(inner.Witness);
            skipped.Concat(from r in inner.Skipped select witnessMapper(r));
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
        public static TestResult<R> Pass(R witness)
        {
            var res = new TestResult<R>();
            res.outcome = Outcome.Passed;
            res.witnessOpt = witness;
            res.testNum++;
            return res;
        }

        /// <summary>
        /// Reports a failing test case with the given witness.
        /// </summary>
        /// <param name="witness">
        /// The trace of the witness that generated a failure.
        /// </param>
        /// <returns>
        /// A test result representing the failure.
        /// </returns>
        public static TestResult<R> Fail(R witness)
        {
            var res = new TestResult<R>();
            res.outcome = Outcome.Failed;
            res.witnessOpt = witness;
            res.testNum++;
            return res;
        }

        /// <summary>
        /// Adds a skipped input to this test result.
        /// </summary>
        /// <param name="toSkip">
        /// The input to skip.
        /// </param>
        public void Skip(R toSkip)
        {
            skipped.Enqueue(toSkip);
            testNum++;
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
            var name = me.Name;
            sb.AppendLine(name);
            sb.Append('=', name.Length);
            sb.AppendLine();
            sb.AppendLine();

            if (me.Succeeded)
            {
                sb.Append("  Passed after ");
                CShowable<int>.Show(me.TestCount, sb);
                sb.AppendLine(" tests.");
            }
            else
            {
                sb.Append("  Failed at test ");
                CShowable<int>.Show(me.TestCount, sb);
                sb.AppendLine(":");
                sb.Append("    ");
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
                    sb.Append("    - ");
                    ShowableR.Show(skipped, sb);
                    sb.AppendLine();
                }
            }
        }
    }

}
