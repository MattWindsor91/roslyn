using System;
using System.Text;

// Concept and basic instances for pretty-printing.

namespace SerialPBT
{
    /// <summary>
    /// Concept for things with a pretty-printer.
    /// </summary>
    public concept CShowable<T>
    {
        void Show(T t, StringBuilder sb);
    }

    /// <summary>
    /// Instance to allow Booleans to be pretty-printed.
    /// </summary>
    public instance ShowableBool : CShowable<bool>
    {
        void Show(bool i, StringBuilder sb) => sb.Append(i);
    }

    /// <summary>
    /// Instance to allow integers to be pretty-printed.
    /// </summary>
    public instance ShowableInt : CShowable<int>
    {
        void Show(int i, StringBuilder sb) => sb.Append(i);
    }

    /// <summary>
    /// Instance to allow arrays of showable items to be pretty-printed.
    /// </summary>
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

    /// <summary>
    /// Helper functions for using showable items.
    /// </summary>
    static class ShowableHelpers
    {
        /// <summary>
        /// Outputs a showable item as a string.
        /// </summary>
        /// <typeparam name="A">
        /// Type of showable items.
        /// </typeparam>
        /// <typeparam name="ShowableA">
        /// The CShowable instance for the item.
        /// </typeparam>
        /// <param name="a">
        /// The item to show.
        /// </param>
        /// <returns>
        /// The string representation of the item.
        /// </returns>
        public static string String<A, implicit ShowableA>(A a)
            where ShowableA : CShowable<A>
        {
            var sb = new StringBuilder();
            ShowableA.Show(a, sb);
            return sb.ToString();
        }

        /// <summary>
        /// Outputs a showable item to the console with a newline.
        /// </summary>
        /// <typeparam name="A">
        /// Type of showable items.
        /// </typeparam>
        /// <typeparam name="ShowableA">
        /// The CShowable instance for the item.
        /// </typeparam>
        /// <param name="a">
        /// The item to show.
        /// </param>
        public static void WriteLine<A, implicit ShowableA>(A a)
            where ShowableA : CShowable<A>
        {
            Console.WriteLine(String(a));
        }
    }
}
