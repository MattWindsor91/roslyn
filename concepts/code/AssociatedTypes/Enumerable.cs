using System;
using System.Collections;
using System.Collections.Generic;
using System.Concepts;
using System.Linq;

/// <summary>
/// Testbed for associated types.
/// </summary>
namespace AssociatedTypes
{
    using static Utils;

    public static class Utils
    {
        /// <summary>
        ///     Constructs an enumerable for any <see cref="CEnumerable"/>.
        /// </summary>
        /// <param name="c">
        ///     The container to be enumerated.
        /// </param>
        /// <typeparam name="C">
        ///     The type to be enumerated.
        /// </typeparam>
        /// <typeparam name="E">
        ///     The element returned by the enumerator.
        /// </typeparam>
        /// <typeparam name="S">
        ///     The state held by the enumerator.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IEnumerable"/> for <see cref="c"/>.
        /// </returns>
        public static IEnumerable<E> Enumerate<C, [AssociatedType] E, [AssociatedType] S, implicit N>(C c) where N : CEnumerable<C, E, S> => new EnumerableShim<C>(c);

        public static void Foreach<C, [AssociatedType] E, [AssociatedType] S, implicit N>(C c, Action<E> f)
            where N : CEnumerable<C, E, S>
        {
            S state = N.GetEnumerator(c);
            while (true)
            {
                if (!N.MoveNext(ref state)) return;
                f(N.Current(ref state));
            }
        }
    }

    /// <summary>
    ///     An inclusive integer range with given start, end, and step.
    /// </summary>
    public struct Range
    {
        /// <summary>
        ///     The start, inclusive, of this range.
        /// </summary>
        public int start;

        /// <summary>
        ///     The end, inclusive, of this range.
        /// </summary>
        public int end;

        /// <summary>
        ///     The step of this range.
        /// </summary>
        public int step;
    }

    /// <summary>
    ///     Concept for types which may be addressed by an integer index.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be addressed by index.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the indexing operation.
    /// </typeparam>
    public concept CIndexable<C, [AssociatedType] E>
    {
        /// <summary>
        ///     Gets the element at a given index.
        /// </summary>
        /// <param name="container">
        ///     The container being accessed.
        /// </param>
        /// <param name="i">
        ///     The index being accessed.
        /// </param>
        /// <returns>
        ///     The <paramref name="i"/>th element, or an exception if out of
        ///     bounds.
        /// </returns>
        E At(C container, int i);
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for arrays, using
    ///     array-element indexing.
    /// </summary>
    /// <typeparam name="E">
    ///     The array element.
    /// </typeparam>
    public instance CIndexableArray<E> : CIndexable<E[], E>
    {
        E At(E[] container, int i) => container[i];
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for strings, using
    ///     character indexing.
    /// </summary>
    public instance CIndexableString : CIndexable<string, char>
    {
        char At(string container, int i) => container[i];
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for bit arrays, using
    ///     bitwise indexing.
    /// </summary>
    public instance CIndexableBitArray : CIndexable<BitArray, bool>
    {
        bool At(BitArray container, int i) => container[i];
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for ranges, calculating the
    ///     indexed term in the bounded arithmetic sequence.
    /// </summary>
    public instance CIndexableRange : CIndexable<Range, int>
    {
        int At(Range range, int n) => range.start + (range.step * n);
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for zipping a tuple of
    ///     indexables into an indexable of tuples.
    /// </summary>
    /// <typeparam name="A">
    ///     The first container.
    /// </typeparam>
    /// <typeparam name="AE">
    ///     The first element.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The second container.
    /// </typeparam>
    /// <typeparam name="BE">
    ///     The second container.
    /// </typeparam>
    public instance CIndexableZip2<A, [AssociatedType] AE, B, [AssociatedType] BE, implicit IA, implicit IB> : CIndexable<(A, B), (AE, BE)>
        where IA : CIndexable<A, AE>
        where IB : CIndexable<B, BE>
    {
        (AE, BE) At((A, B) tup, int i) => (IA.At(tup.Item1, i), IB.At(tup.Item2, i));
    }

    /// <summary>
    ///     Concept for types which have a length, or upper bound on indexing.
    /// </summary>
    /// <typeparam name="C">
    ///     The type whose length is to be assessed.
    /// </typeparam>
    public concept CLength<C>
    {
        /// <summary>
        ///     Returns the length of a given container.
        /// </summary>
        /// <param name="container">
        ///     The item whose length is to be assessed.
        /// </param>
        /// <returns>
        ///     The length of <paramref name="c"/>.
        /// </returns>
        int Length(C container);
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for arrays, using array length.
    /// </summary>
    /// <typeparam name="E">
    ///     The array element.
    /// </typeparam>
    public instance CLengthArray<E> : CLength<E[]>
    {
        int Length(E[] container) => container.Length;
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for strings, using string length.
    /// </summary>
    public instance CLengthString : CLength<string>
    {
        int Length(string container) => container.Length;
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for bit arrays, using bit length.
    /// </summary>
    public instance CLengthBitArray : CLength<BitArray>
    {
        int Length(BitArray container) => container.Length;
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for ranges, calculating the
    ///     number of terms in the bounded arithmetic sequence.
    /// </summary>
    public instance CLengthRange : CLength<Range>
    {
        int Length(Range range) => ((range.end - range.start) / range.step) + 1;
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for zipping a tuple of lengths
    ///     into the length of a tuple, taking the minimum of both lengths.
    /// </summary>
    /// <typeparam name="A">
    ///     The first container.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The second container.
    /// </typeparam>
    public instance CLengthZip2<A, B, implicit LA, implicit LB> : CLength<(A, B)>
        where LA : CLength<A>
        where LB : CLength<B>
    {
        int Length((A, B) tup) => Math.Min(LA.Length(tup.Item1), LB.Length(tup.Item2));
    }

    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    public concept CEnumerator<E, [AssociatedType] S>
    {
        void Reset(ref S enumerator);
        bool MoveNext(ref S enumerator);
        E Current(ref S enumerator);
        void Dispose(ref S enumerator);
    }

    /// <summary>
    ///     Concept for types which may be enumerated.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    public concept CEnumerable<C, [AssociatedType] E, [AssociatedType] S> : CEnumerator<E, S>
    {
        S GetEnumerator(C container);
    }


    /// <summary>
    ///     Unspecialised implementation of CEnumerable based on CLength and
    ///     CIndexable, and perf benchmarks for it.
    /// </summary>
    static class Unspecialised
    {
        /// <summary>
        ///     Instance of <see cref="CEnumerable"/> converting any indexable
        ///     collection with a length into an enumerator.
        /// </summary>
        /// <typeparam name="C">
        ///     The type to be enumerated.
        /// </typeparam>
        /// <typeparam name="E">
        ///     The element returned by the enumerator.
        /// </typeparam>
        public instance CEnumerableLE<C, [AssociatedType] E, implicit I, implicit L> : CEnumerable<C, E, (C, int, E)>
            where I : CIndexable<C, E>
            where L : CLength<C>
        {
            (C, int, E) GetEnumerator(C container) => (container, -1, default(E));
            void Reset(ref (C, int, E) enumerator)
            {
                enumerator.Item2 = -1;
                enumerator.Item3 = default(E);
            }
            bool MoveNext(ref (C, int, E) enumerator)
            {
                if (++enumerator.Item2 >= L.Length(enumerator.Item1)) return false;
                enumerator.Item3 = I.At(enumerator.Item1, enumerator.Item2);
                return true;
            }
            E Current(ref (C, int, E) enumerator) => enumerator.Item3;
            void Dispose(ref (C, int, E) enumerator) {}
        }

        public static void RunWordTest(string[] words1, string[] words2, int[][] scores, int runs)
        {
            Enumerate("xyzzy");
            Enumerate(("abcdefghijklmnopqrstuvwxyz", new int[] { }));
            WordTest wt = new WordTest(words1, words2, scores, runs);

            double cenumerableShimTotalTime = wt.RunCEnumerableShim();
            Console.Out.WriteLine($"TOTAL (CEnumerable Shim):     {cenumerableShimTotalTime}s");

            double cenumerableForeachTotalTime = wt.RunCEnumerableForeach();
            Console.Out.WriteLine($"TOTAL (CEnumerable Foreach):  {cenumerableForeachTotalTime}s");

            double cenumerableUnrolledTotalTime = wt.RunCEnumerableUnrolled();
            Console.Out.WriteLine($"TOTAL (CEnumerable Unrolled): {cenumerableUnrolledTotalTime}s");
        }

        public class WordTest
        {
            private string[] words1;
            private string[] words2;
            private int[][] scores;
            private int runs;

            public WordTest(string[] words1, string[] words2, int[][] scores, int runs)
            {
                this.words1 = words1;
                this.words2 = words2;
                this.scores = scores;
                this.runs = runs;
            }

            public double RunCEnumerableShim()
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"cenumerableshim.txt"))
                {
                    Timer t = new Timer();
                    for (int i = 0; i < runs; i++)
                    {
                        foreach (var tup1 in Enumerate((words1, words2)))
                        {
                            var ltotal = 0;
                            var rtotal = 0;

                            foreach (var score in Enumerate(scores))
                            {
                                var lcount = 0;
                                var rcount = 0;

                                foreach (var tup2 in Enumerate(("abcdefghijklmnopqrstuvwxyz", score)))
                                {
                                    foreach (var letter in Enumerate(tup1.Item1))
                                    {
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    }
                                    foreach (var letter in Enumerate(tup1.Item2))
                                    {
                                        if (letter == tup2.Item1) rcount += tup2.Item2;
                                    }
                                }

                                ltotal += lcount;
                                rtotal += rcount;

                                if (lcount > rcount) file.Write($"{tup1.Item1} ");
                                if (lcount < rcount) file.Write($"{tup1.Item2} ");
                                if (lcount == rcount) file.Write("draw ");
                            }

                            if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                            if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                            if (ltotal == rtotal) file.Write("-> draw");
                            file.WriteLine();
                        }
                    }
                    return t.Check();
                }
            }

            public double RunCEnumerableForeach()
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"cenumerableforeach.txt"))
                {
                    Timer t = new Timer();
                    for (int i = 0; i < runs; i++)
                    {
                        Foreach((words1, words2), tup1 =>
                        {
                            var ltotal = 0;
                            var rtotal = 0;

                            Foreach(scores, score =>
                            {
                                var lcount = 0;
                                var rcount = 0;

                                Foreach(("abcdefghijklmnopqrstuvwxyz", score), tup2 =>
                                {
                                    Foreach((tup1.Item1), letter =>
                                    {
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    });
                                    Foreach((tup1.Item2), letter =>
                                    {
                                        if (letter == tup2.Item1) rcount += tup2.Item2;
                                    });
                                });

                                ltotal += lcount;
                                rtotal += rcount;

                                if (lcount > rcount) file.Write($"{tup1.Item1} ");
                                if (lcount < rcount) file.Write($"{tup1.Item2} ");
                                if (lcount == rcount) file.Write("draw ");
                            });

                            if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                            if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                            if (ltotal == rtotal) file.Write("-> draw");
                            file.WriteLine();
                        });
                    }
                    return t.Check();
                }
            }

            public double RunCEnumerableUnrolled()
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"cenumerableunrolled.txt"))
                {
                    Timer t = new Timer();
                    for (int i = 0; i < runs; i++)
                    {
                        var state1 = CEnumerable<(string[], string[])>.GetEnumerator((words1, words2));
                        while (true)
                        {
                            if (!CEnumerable<(string[], string[])>.MoveNext(ref state1)) break;
                            var tup1 = CEnumerable<(string[], string[])>.Current(ref state1);

                            var ltotal = 0;
                            var rtotal = 0;

                            var state2 = CEnumerable<int[][]>.GetEnumerator(scores);
                            while (true)
                            {
                                if (!CEnumerable<int[][]>.MoveNext(ref state2)) break;
                                var score = CEnumerable<int[][]>.Current(ref state2);

                                var lcount = 0;
                                var rcount = 0;

                                var state3 = CEnumerable<(string, int[])>.GetEnumerator(("abcdefghijklmnopqrstuvwxyz", score));
                                while (true)
                                {
                                    if (!CEnumerable<(string, int[])>.MoveNext(ref state3)) break;
                                    var tup2 = CEnumerable<(string, int[])>.Current(ref state3);

                                    var state4 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerable<string>.MoveNext(ref state4)) break;
                                        var letter = CEnumerable<string>.Current(ref state4);
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    }
                                    var state5 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerable<string>.MoveNext(ref state5)) break;
                                        var letter = CEnumerable<string>.Current(ref state5);
                                        if (letter == tup2.Item1) rcount += tup2.Item2;
                                    }
                                }

                                ltotal += lcount;
                                rtotal += rcount;

                                if (lcount > rcount) file.Write($"{tup1.Item1} ");
                                if (lcount < rcount) file.Write($"{tup1.Item2} ");
                                if (lcount == rcount) file.Write("draw ");
                            }

                            if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                            if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                            if (ltotal == rtotal) file.Write("-> draw");
                            file.WriteLine();
                        }
                    }
                    return t.Check();
                }
            }
        }
    }

    /// <summary>
    ///     More specialised implementations of CEnumerable, and perf benchmarks
    ///     for them.
    /// </summary>
    static class Specialised
    {
        public instance CEnumerableString : CEnumerable<string, char, (char[], int, char)>
        {
            (char[], int, char) GetEnumerator(string str) => (str.ToCharArray(), -1, default(char));
            void Reset(ref (char[], int, char) enumerator)
            {
                enumerator.Item2 = -1;
                enumerator.Item3 = default(char);
            }
            bool MoveNext(ref (char[], int, char) enumerator)
            {
                if (++enumerator.Item2 >= (enumerator.Item1.Length)) return false;
                enumerator.Item3 = enumerator.Item1[enumerator.Item2];
                return true;
            }
            char Current(ref (char[], int, char) enumerator) => enumerator.Item3;
            void Dispose(ref (char[], int, char) enumerator) {}
        }

        public instance CEnumerableArray<E> : CEnumerable<E[], E, (E[], int, E)>
        {
            (E[], int, E) GetEnumerator(E[] ary) => (ary, -1, default(E));
            void Reset(ref (E[], int, E) enumerator)
            {
                enumerator.Item2 = -1;
                enumerator.Item3 = default(E);
            }
            bool MoveNext(ref (E[], int, E) enumerator)
            {
                if (++enumerator.Item2 >= (enumerator.Item1.Length)) return false;
                enumerator.Item3 = enumerator.Item1[enumerator.Item2];
                return true;
            }
            E Current(ref (E[], int, E) enumerator) => enumerator.Item3;
            void Dispose(ref (E[], int, E) enumerator) { }
        }

        /// <summary>
        ///     Instance of <see cref="CEnumerable"/> for zipping a tuple of
        ///     enumerables into an enumerables of tuples.
        /// </summary>
        /// <typeparam name="A">
        ///     The first container.
        /// </typeparam>
        /// <typeparam name="AE">
        ///     The first element.
        /// </typeparam>
        /// <typeparam name="AS">
        ///     The first enumerator state.
        /// </typeparam>
        /// <typeparam name="B">
        ///     The second container.
        /// </typeparam>
        /// <typeparam name="BE">
        ///     The second container.
        /// </typeparam>
        /// <typeparam name="BS">
        ///     The second enumerator state.
        /// </typeparam>
        public instance CEnumerableZip2<A, [AssociatedType] AE, [AssociatedType] AS,
                                        B, [AssociatedType] BE, [AssociatedType] BS,
                                        implicit EA, implicit EB>
                                        : CEnumerable<(A, B), (AE, BE), (AS, BS)>
            where EA : CEnumerable<A, AE, AS>
            where EB : CEnumerable<B, BE, BS>
        {
            (AS, BS) GetEnumerator((A, B) tup) =>
                (EA.GetEnumerator(tup.Item1), EB.GetEnumerator(tup.Item2));
            void Reset(ref (AS, BS) tup)
            {
                EA.Reset(ref tup.Item1);
                EB.Reset(ref tup.Item2);
            }
            bool MoveNext(ref (AS, BS) tup)
            {
                if (!EA.MoveNext(ref tup.Item1)) return false;
                return EB.MoveNext(ref tup.Item2);
            }
            (AE, BE) Current(ref (AS, BS) tup) =>
                (EA.Current(ref tup.Item1), EB.Current(ref tup.Item2));
            void Dispose(ref (AS, BS) tup)
            {
                EA.Dispose(ref tup.Item1);
                EB.Dispose(ref tup.Item2);
            }
        }

        public static void RunWordTest(string[] words1, string[] words2, int[][] scores, int runs)
        {
            WordTest wt = new WordTest(words1, words2, scores, runs);

            double cenumerableShimTotalTime = wt.RunCEnumerableShim();
            Console.Out.WriteLine($"TOTAL (CEnumerable Shim):     {cenumerableShimTotalTime}s");

            double cenumerableForeachTotalTime = wt.RunCEnumerableForeach();
            Console.Out.WriteLine($"TOTAL (CEnumerable Foreach):  {cenumerableForeachTotalTime}s");

            double cenumerableUnrolledTotalTime = wt.RunCEnumerableUnrolled();
            Console.Out.WriteLine($"TOTAL (CEnumerable Unrolled): {cenumerableUnrolledTotalTime}s");
        }

        // Ideally this wouldn't be duplicated, but I was getting weird type
        // inference issues when generalising this, and gave up on trying to fix
        // them for now.

        public class WordTest
        {
            private string[] words1;
            private string[] words2;
            private int[][] scores;
            private int runs;

            public WordTest(string[] words1, string[] words2, int[][] scores, int runs)
            {
                this.words1 = words1;
                this.words2 = words2;
                this.scores = scores;
                this.runs = runs;
            }

            public double RunCEnumerableShim()
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"cenumerableshim.txt"))
                {
                    Timer t = new Timer();
                    for (int i = 0; i < runs; i++)
                    {
                        foreach (var tup1 in Enumerate((words1, words2)))
                        {
                            var ltotal = 0;
                            var rtotal = 0;

                            foreach (var score in Enumerate(scores))
                            {
                                var lcount = 0;
                                var rcount = 0;

                                foreach (var tup2 in Enumerate(("abcdefghijklmnopqrstuvwxyz", score)))
                                {
                                    foreach (var letter in Enumerate(tup1.Item1))
                                    {
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    }
                                    foreach (var letter in Enumerate(tup1.Item2))
                                    {
                                        if (letter == tup2.Item1) rcount += tup2.Item2;
                                    }
                                }

                                ltotal += lcount;
                                rtotal += rcount;

                                if (lcount > rcount) file.Write($"{tup1.Item1} ");
                                if (lcount < rcount) file.Write($"{tup1.Item2} ");
                                if (lcount == rcount) file.Write("draw ");
                            }

                            if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                            if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                            if (ltotal == rtotal) file.Write("-> draw");
                            file.WriteLine();
                        }
                    }
                    return t.Check();
                }
            }

            public double RunCEnumerableForeach()
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"cenumerableforeach.txt"))
                {
                    Timer t = new Timer();
                    for (int i = 0; i < runs; i++)
                    {
                        Foreach((words1, words2), tup1 =>
                        {
                            var ltotal = 0;
                            var rtotal = 0;

                            Foreach(scores, score =>
                            {
                                var lcount = 0;
                                var rcount = 0;

                                Foreach(("abcdefghijklmnopqrstuvwxyz", score), tup2 =>
                                {
                                    Foreach((tup1.Item1), letter =>
                                    {
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    });
                                    Foreach((tup1.Item2), letter =>
                                    {
                                        if (letter == tup2.Item1) rcount += tup2.Item2;
                                    });
                                });

                                ltotal += lcount;
                                rtotal += rcount;

                                if (lcount > rcount) file.Write($"{tup1.Item1} ");
                                if (lcount < rcount) file.Write($"{tup1.Item2} ");
                                if (lcount == rcount) file.Write("draw ");
                            });

                            if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                            if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                            if (ltotal == rtotal) file.Write("-> draw");
                            file.WriteLine();
                        });
                    }
                    return t.Check();
                }
            }

            public double RunCEnumerableUnrolled()
            {
                using (System.IO.StreamWriter file =
                    new System.IO.StreamWriter(@"cenumerableunrolled.txt"))
                {
                    Timer t = new Timer();
                    for (int i = 0; i < runs; i++)
                    {
                        var state1 = CEnumerable<(string[], string[])>.GetEnumerator((words1, words2));
                        while (true)
                        {
                            if (!CEnumerable<(string[], string[])>.MoveNext(ref state1)) break;
                            var tup1 = CEnumerable<(string[], string[])>.Current(ref state1);

                            var ltotal = 0;
                            var rtotal = 0;

                            var state2 = CEnumerable<int[][]>.GetEnumerator(scores);
                            while (true)
                            {
                                if (!CEnumerable<int[][]>.MoveNext(ref state2)) break;
                                var score = CEnumerable<int[][]>.Current(ref state2);

                                var lcount = 0;
                                var rcount = 0;

                                var state3 = CEnumerable<(string, int[])>.GetEnumerator(("abcdefghijklmnopqrstuvwxyz", score));
                                while (true)
                                {
                                    if (!CEnumerable<(string, int[])>.MoveNext(ref state3)) break;
                                    var tup2 = CEnumerable<(string, int[])>.Current(ref state3);

                                    var state4 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerable<string>.MoveNext(ref state4)) break;
                                        var letter = CEnumerable<string>.Current(ref state4);
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    }
                                    var state5 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerable<string>.MoveNext(ref state5)) break;
                                        var letter = CEnumerable<string>.Current(ref state5);
                                        if (letter == tup2.Item1) rcount += tup2.Item2;
                                    }
                                }

                                ltotal += lcount;
                                rtotal += rcount;

                                if (lcount > rcount) file.Write($"{tup1.Item1} ");
                                if (lcount < rcount) file.Write($"{tup1.Item2} ");
                                if (lcount == rcount) file.Write("draw ");
                            }

                            if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                            if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                            if (ltotal == rtotal) file.Write("-> draw");
                            file.WriteLine();
                        }
                    }
                    return t.Check();
                }
            }
        }
    }

    /// <summary>
    ///     Adaptor converting <see cref="CEnumerable"/> into
    ///     <see cref="IEnumerator"/>.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    class EnumeratorShim<C, E, S, implicit N> : IEnumerator<E>
        where N : CEnumerable<C, E, S>
    {
        private S _state;

        /// <summary>
        ///     Creates an enumerator for the given collection.
        /// </summary>
        /// <param name="collection">
        ///     The collection to be enumerated.
        /// </param>
        public EnumeratorShim(C collection)
        {
            _state = N.GetEnumerator(collection);
        }

        public E Current => N.Current(ref _state);
        object IEnumerator.Current => N.Current(ref _state);
        public bool MoveNext() => N.MoveNext(ref _state);
        public void Reset() { N.Reset(ref _state); }
        void IDisposable.Dispose() { N.Dispose(ref _state); }
    }

    /// <summary>
    ///     Adaptor converting <see cref="CEnumerable"/> into
    ///     <see cref="IEnumerable"/>.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    class EnumerableShim<C, [AssociatedType] E, [AssociatedType] S, implicit N> : IEnumerable<E>
        where N : CEnumerable<C, E, S>
    {
        private C _collection;

        /// <summary>
        ///     Creates an enumerable for the given collection.
        /// </summary>
        /// <param name="collection">
        ///     The collection to be enumerated.
        /// </param>
        public EnumerableShim(C collection)
        {
            _collection = collection;
        }

        public IEnumerator<E> GetEnumerator() => new EnumeratorShim<C, E, S>(_collection);
        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorShim<C, E, S>(_collection);
    }

    public class Timer
    {
        private DateTime start;

        public Timer()
        {
            start = DateTime.Now;
        }

        public double Check()
        {
            TimeSpan dur = DateTime.Now - start;
            return dur.TotalSeconds;
        }
    }

    public class EnumerableTest
    { 
        public static void Run()
        {
            // The idea of this test is to get a rough idea of how CEnumerable
            // compares to the current IEnumerable/foreach situation.  We
            // benchmark the following:
            //
            // 1/ IEnumerable/foreach using Zip to pair up tuples;
            // 2/ CEnumerable using the shim classes to convert to IEnumerable
            //    and then foreach;
            // 3/ CEnumerable using Foreach, a higher-order function that
            //    directly invokes the enumerator using a delegate on each item;
            // 4/ An unrolled form of 3/ with no overhead from delegates.
            //
            // We run 2--4 twice: once using 'unspecialised' instances based on
            // the CLength and CIndexable instances for strings, arrays, and
            // tuples; and again using clunkier but more direct instances.

            var words1 = new string[]
            {
                "abominable",
                "basic",
                "ceilidh",
                "dare",
                "euphemistic",
                "forlorn",
                "glaringly",
                "harpsichord",
                "incandescent",
                "jalopy",
                "kaleidoscope",
                "lament",
                "manhandled",
                "nonsence",
                "original",
                "pylon",
                "quench",
                "robust",
                "stomach",
                "tyre",
                "unambiguous",
                "valence",
                "whataboutism",
                "xenophobe",
                "yottabyte",
                "zenith"
            };
            var words2 = new string[]
            {
                "archway",
                "balham",
                "cambridge",
                "dorchester",
                "erith",
                "finchley",
                "grantham",
                "hull",
                "islington",
                "jersey",
                "kent",
                "leeds",
                "manchester",
                "norwich",
                "oxford",
                "peterborough",
                "queenborough-in-sheppey",
                "royston",
                "stevenage",
                "tunbridge wells",
                "ullapool",
                "vauxhall",
                "westminster",
                "xuchang",
                "york",
                "zaire"
            };

            var tileScore = new int[26]
            {
                1, 3, 3, 2, 1, 4, 2, 4, 1, 8, 5, 1, 3, 1, 1, 3, 10, 1, 1, 1, 1, 4, 4, 8, 4, 10
            };
            var vowelScore = new int[26]
            {
                1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0
            };
            var consonantScore = new int[26]
            {
                0, 1, 1, 1, 0, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1
            };
            var aScore = new int[26]
            {
                1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
            };
            var scores = new int[][] { tileScore, vowelScore, consonantScore, aScore };

            int runs = 1000;

            double ienumerableTotalTime = RunIEnumerable(words1, words2, scores, runs);
            Console.Out.WriteLine($"TOTAL (IEnumerable+Zip):      {ienumerableTotalTime}s");

            Unspecialised.RunWordTest(words1, words2, scores, runs);
            Specialised.RunWordTest(words1, words2, scores, runs);
        }

        static double RunIEnumerable(string[] words1, string[] words2, int[][] scores, int runs)
        {
            using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"ienumerable.txt"))
            {
                Timer t = new Timer();
                for (int i = 0; i < runs; i++)
                {
                    foreach (var tup1 in Enumerable.Zip(words1, words2, (x, y) => (x, y)))
                    {
                        var ltotal = 0;
                        var rtotal = 0;

                        foreach (var score in scores)
                        {
                            var lcount = 0;
                            var rcount = 0;

                            foreach (var tup2 in Enumerable.Zip("abcdefghijklmnopqrstuvwxyz", score, (x, y) => (x, y)))
                            {
                                foreach (var letter in tup1.Item1)
                                {
                                    if (letter == tup2.Item1) lcount += tup2.Item2;
                                }
                                foreach (var letter in tup1.Item2)
                                {
                                    if (letter == tup2.Item1) rcount += tup2.Item2;
                                }
                            }

                            ltotal += lcount;
                            rtotal += rcount;

                            if (lcount > rcount) file.Write($"{tup1.Item1} ");
                            if (lcount < rcount) file.Write($"{tup1.Item2} ");
                            if (lcount == rcount) file.Write("draw ");
                        }

                        if (ltotal > rtotal) file.Write($"-> {tup1.Item1}");
                        if (ltotal < rtotal) file.Write($"-> {tup1.Item2}");
                        if (ltotal == rtotal) file.Write("-> draw");
                        file.WriteLine();
                    }
                }
                return t.Check();
            }
        }
    }
}
