using System;
using System.Text;

namespace SerialPBT
{
    /// <summary>
    /// Concept for things with a pretty-printer.
    /// </summary>
    public concept CShowable<T>
    {
        void Show(T t, StringBuilder sb);
    }

    public instance ShowableBool : CShowable<bool>
    {
        void Show(bool i, StringBuilder sb) => sb.Append(i);
    }

    public instance ShowableInt : CShowable<int>
    {
        void Show(int i, StringBuilder sb) => sb.Append(i);
    }

    public instance ShowableArray<A, implicit ShowableA> : CShowable<A[]>
        where ShowableA : CShowable<A>
    {
        void Show(A[] xs, StringBuilder sb)
        {
            sb.Append("[");

            var xl = xs.Length;
            for (var i = 0; i < xl; i++)
            {
                if (0 < i)
                {
                    sb.Append(", ");
                }

                ShowableA.Show(xs[i], sb);
            }

            sb.Append("]");
        }
    }

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

    static class ShowableHelpers
    {
        public static string String<A, implicit ShowableA>(A a)
            where ShowableA : CShowable<A>
        {
            var sb = new StringBuilder();
            ShowableA.Show(a, sb);
            return sb.ToString();
        }

        public static void Write<A, implicit ShowableA>(A a)
            where ShowableA : CShowable<A>
        {
            Console.WriteLine(String(a));
        }
    }
}
