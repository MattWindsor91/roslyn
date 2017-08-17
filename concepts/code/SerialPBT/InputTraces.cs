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
    /// Part of an input trace that corresponds to an implication.
    /// <para>
    /// This trace element records whether the test case was skipped due to
    /// the implication failing at this point, and, if not, the rest of the
    /// input trace.
    /// </para>
    /// </summary>
    /// <typeparam name="R">
    /// Type of the rest of the input trace.
    /// </typeparam>
    public struct ImpTrace<R>
    {
        /// <summary>
        /// True if the filter failed, causing this candidate to be skipped.
        /// </summary>
        public bool skipped;
        /// <summary>
        /// The rest of the input trace continuing past the implication.
        /// </summary>
        public R next;
    }

    /// <summary>
    /// Inductive case for showing an implication trace, when the
    /// tail of the trace is End.
    /// </summary>
    public instance ShowableImpTrace_Base : CShowable<ImpTrace<EndTrace>>
    {
        void Show(ImpTrace<EndTrace> trace, StringBuilder sb)
        {
        }
    }
    /// <summary>
    /// Inductive case for showing an implication trace, when the
    /// tail of the trace is another trace.
    /// </summary>
    public instance ShowableImpTrace_Inductive<R, implicit ShowableR> : CShowable<ImpTrace<R>>
        where ShowableR : CShowable<R>
    {
        void Show(ImpTrace<R> trace, StringBuilder sb)
        {
            if (trace.skipped)
            {
                sb.Append("(filtered)");
                return;
            }
            ShowableR.Show(trace.next, sb);
        }
    }

    /// <summary>
    /// An input trace showing the application of a function of arity 1.
    /// </summary>
    /// <typeparam name="A">
    /// Type of the input to the function.
    /// </typeparam>
    /// <typeparam name="R">
    /// Type of the rest of the input trace.
    /// </typeparam>
    public struct F1Trace<A, R>
    {
        public A input;
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
            sb.Append(" -> ");
            ShowableR.Show(trace.next, sb);
        }
    }

    /// <summary>
    /// An input trace showing the application of a function of arity 2.
    /// </summary>
    /// <typeparam name="A">
    /// Type of the first input to the function.
    /// </typeparam>
    /// <typeparam name="B">
    /// Type of the second input to the function.
    /// </typeparam>
    /// <typeparam name="R">
    /// Type of the rest of the input trace.
    /// </typeparam>
    public struct F2Trace<A, B, R>
    {
        public A input1;
        public B input2;
        public R next;
    }

    /// <summary>
    /// Base case for showing an arity-2 function application trace, when the
    /// tail of the trace is End.
    /// </summary>
    public instance ShowableF2Trace_Base<A, B, implicit ShowableA, implicit ShowableB> : CShowable<F2Trace<A, B, EndTrace>>
        where ShowableA : CShowable<A>
        where ShowableB : CShowable<B>
    {
        void Show(F2Trace<A,B,  EndTrace> trace, StringBuilder sb)
        {
            ShowableA.Show(trace.input1, sb);
            sb.Append(", ");
            ShowableB.Show(trace.input2, sb);
        }
    }

    /// <summary>
    /// Inductive case for showing an arity-2 function application trace,
    /// when the tail of the trace is another trace.
    /// </summary>
    public instance ShowableF2Trace_Inductive<A, B, R, implicit ShowableA, implicit ShowableB, implicit ShowableR> : CShowable<F2Trace<A, B, R>>
        where ShowableA : CShowable<A>
        where ShowableB : CShowable<B>
        where ShowableR : CShowable<R>
    {
        void Show(F2Trace<A, B, R> trace, StringBuilder sb)
        {
            ShowableA.Show(trace.input1, sb);
            sb.Append(", ");
            ShowableB.Show(trace.input2, sb);
            sb.Append(" -> ");
            ShowableR.Show(trace.next, sb);
        }
    }
}
