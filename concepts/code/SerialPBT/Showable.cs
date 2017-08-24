using System.Concepts.Showable;
using System.Text;

// Additional Showable instances not in the concepts library.

namespace SerialPBT
{
    /// <summary>
    /// Instance to allow 2-tuples of showable items to be pretty-printed.
    /// </summary>
    public instance ShowableVTup2<A, B, implicit ShowableA, implicit ShowableB> : CShowable<(A, B)>
        where ShowableA : CShowable<A>
        where ShowableB : CShowable<B>
    {
        void Show((A, B) tup, StringBuilder sb)
        {
            sb.Append("(");
            ShowableA.Show(tup.Item1, sb);
            sb.Append(", ");
            ShowableB.Show(tup.Item2, sb);
            sb.Append(")");
        }
    }
}
