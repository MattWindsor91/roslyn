using System;
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
}
