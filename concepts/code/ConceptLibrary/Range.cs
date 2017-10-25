using System;
using System.Concepts.Enumerable;
using System.Concepts.Prelude;
using System.Text;

namespace System.Concepts
{
    /// <summary>
    /// A generic range.
    /// </summary>
    /// <typeparam name="TNum">
    /// Type of numbers in the range.
    /// </typeparam>
    public struct Range<TNum>
    {
        /// <summary>
        /// The start of the range.
        /// </summary>
        public TNum start;
        /// <summary>
        /// The number of items in the range.
        /// </summary>
        public int count;

        public TNum At<implicit N>(int pos) where N : Num<TNum> => start + N.FromInteger(pos);

        public TNum End<implicit N>() where N : Num<TNum> => start + N.FromInteger(count);
    }


    /// <summary>
    /// Instance for O(1) length lookup of ranges.
    /// </summary>
    /// <typeparam name="TNum">
    /// Type of the number in the range.
    /// </typeparam>
    public instance StaticCountable_Range<TNum> : Countable.CStaticCountable<Range<TNum>>
    {
        int Count(this Range<TNum> t) => t.count;
    }

    /// <summary>
    /// Instance for indexing on ranges.
    /// </summary>
    /// <typeparam name="TNum">
    /// Type of the number in the range.
    /// </typeparam>
    public instance Indexable_Range<TNum, implicit N> : Indexable.CIndexable<Range<TNum>, int, TNum>
        where N : Num<TNum>
    {
        // TODO(@MattWindsor91): we should be able to autofill At, but can't,
        // probably because it has an extra witness type parameter.
        TNum At(this Range<TNum> r, int pos) => r.At(pos);
    }

    public instance Showable_Range<TNum, implicit N, implicit S> : Showable.CShowable<Range<TNum>>
        where N : Num<TNum>
        where S : Showable.CShowable<TNum>
    {
        void Show(Range<TNum> r, StringBuilder sb)
        {
            sb.Append("[");
            S.Show(r.start, sb);
            sb.Append(", ");
            S.Show(r.End(), sb);
            sb.Append("]");
        }
    }

    #region Enumeration
    public struct RangeCursor<TNum>
    {
        public enum State
        {
            OneBefore,
            Okay,
            OneAfter
        }

        // NOTE(@MattWindsor91): range is deliberately inlined, to avoid
        //     having to do indirect field reaches on it.


        /// <summary>The start of the range.</summary>
        public readonly TNum start;
        /// <summary>The end of the range.</summary>
        public readonly TNum end;
        /// <summary>The initial state of the cursor.</summary>
        public readonly State initialState;


        /// <summary>
        /// The current item in the range.
        /// </summary>
        public TNum current;
        /// <summary>
        /// The state of the cursor.
        /// </summary>
        public State state;

        public RangeCursor(TNum start, TNum end, State initialState)
        {
            this.start = start;
            this.end = end;
            state = this.initialState = initialState;
            current = default;
        }
    }

    /// <summary>
    /// Range cursors are cloneable enumerators.
    /// </summary>
    public instance CloneableEnumerator_Range<TNum, implicit N, implicit E> : CCloneableEnumerator<RangeCursor<TNum>, TNum>
        where N : Num<TNum>
        where E : Eq<TNum>
    {
        // TODO: catch inverted ranges and overflows
        // TODO: better optimisation if range is empty
        RangeCursor<TNum> Clone(ref this RangeCursor<TNum> e) =>
            new RangeCursor<TNum>(e.start, e.end, e.initialState);

        void Reset(ref this RangeCursor<TNum> e)
        {
            // No need to rewind current.
            // We do that lazily in MoveNext when we're AtStart.
            e.state = e.initialState;
        }

        bool MoveNext(ref this RangeCursor<TNum> e)
        {
            switch (e.state)
            {
                case RangeCursor<TNum>.State.Okay:
                    e.current += FromInteger(1);
                    if (e.end != e.current)
                    {
                        return true;
                    }
                    e.state = RangeCursor<TNum>.State.OneAfter;
                    return false;
                case RangeCursor<TNum>.State.OneAfter:
                    return false;
                case RangeCursor<TNum>.State.OneBefore:
                    e.current = e.start;
                    e.state = RangeCursor<TNum>.State.Okay;
                    // Assume we can't have a 0-length range.
                    return true;
            }
            return false;
        }

        TNum Current(ref this RangeCursor<TNum> e) => e.current;
        void Dispose(ref this RangeCursor<TNum> e) { }
    }

    /// <summary>
    /// Various enumerator instances for ranges.
    /// </summary>
    public instance Enumerable_Range<TNum, implicit N, implicit E> : CEnumerable<Range<TNum>, RangeCursor<TNum>>
        where N : Num<TNum>
        where E : Eq<TNum>
    {
        RangeCursor<TNum> GetEnumerator(this Range<TNum> range) =>
            new RangeCursor<TNum>(
                start: range.start,
                end: range.start + FromInteger(range.count),
                initialState: range.count == 0
                    ? RangeCursor<TNum>.State.OneAfter
                    : RangeCursor<TNum>.State.OneBefore);
    }

#endregion Enumeration
}
