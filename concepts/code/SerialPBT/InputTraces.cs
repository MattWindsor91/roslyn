using System.Text;

// Types for input traces.
//
// These record the sets of inputs that led to a failing test, and are used to
// build the output when a test fails.
//
// Each (except for EndTrace) has a corresponding CShowable instance that
// allows them to be printed.

namespace SerialPBT
{
    /// <summary>
    /// Marker for the end of a test input trace.
    /// </summary>
    public struct EndTrace
    {
    }

    // End deliberately does not have a Showable instance, so it can easily be
    // used as the base case in inductive input trace Showable instances.

    /// <summary>
    /// Part of an input trace that corresponds to an existential.
    /// <para>
    /// Since input traces are only used for failures or skipping, and
    /// an existential only has a valid trace when it passes, we don't actually
    /// record any information in the existential trace.
    /// </para>
    /// </summary>
    public struct ExistsTrace
    {
    }

    /// <summary>
    /// Showable instance for outputting an existential in an input trace.
    /// </summary>
    public instance ShowableExistsTrace : CShowable<ExistsTrace>
    {
        void Show(ExistsTrace _, StringBuilder sb)
        {
            sb.Append("(unsatisfiable existential)");
        }
    }

    /// <summary>
    /// An input trace showing the application of a function of arity 1.
    /// </summary>
    /// <typeparam name="A">
    /// The type of the input to the function.
    /// </typeparam>
    /// <typeparam name="R">
    /// The type of the rest of the input trace.
    /// </typeparam>
    public struct F1Trace<A, R>
    {
        public A input;
        public bool skipped;
        public R next;
    }

    /// <summary>
    /// Base case for showing a function application trace, when the
    /// tail of the trace is End.
    /// </summary>
    public instance ShowableF1Trace_Base<A, implicit ShowableA> : CShowable<F1Trace<A, EndTrace>>
        where ShowableA : CShowable<A>
    {
        void Show(F1Trace<A, EndTrace> trace, StringBuilder sb)
        {
            ShowableA.Show(trace.input, sb);
        }
    }

    /// <summary>
    /// Inductive case for showing a function application trace, when the
    /// tail of the trace is another trace.
    /// </summary>
    public instance ShowableF1Trace_Inductive<A, R, implicit ShowableA, implicit ShowableR> : CShowable<F1Trace<A, R>>
        where ShowableA : CShowable<A>
        where ShowableR : CShowable<R>
    {
        void Show(F1Trace<A, R> trace, StringBuilder sb)
        {
            ShowableA.Show(trace.input, sb);
            if (trace.skipped)
            {
                return;
            }

            sb.Append(" -> ");
            ShowableR.Show(trace.next, sb);
        }
    }
}
