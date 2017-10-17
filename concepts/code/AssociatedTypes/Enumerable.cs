using System;
using System.Collections;
using System.Collections.Generic;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Enumerable;
using System.Concepts.Indexable;
using System.Concepts.Prelude;
using static System.Concepts.Enumerable.Instances;
using System.Linq;

/// <summary>
/// Testbed for associated types.
/// </summary>
namespace AssociatedTypes
{
    using System.Concepts.Countable;
    using System.Concepts.Indexable;
    using static Utils;

    public static class Utils
    {
        /// <summary>
        ///     Constructs an enumerable for any <see cref="CEnumerable"/>.
        /// </summary>
        /// <param name="c">
        ///     The container to be enumerated.
        /// </param>
        /// <typeparam name="TColl">
        ///     The type to be enumerated.
        /// </typeparam>
        /// <typeparam name="TState">
        ///     The state held by the enumerator.
        /// </typeparam>
        /// <typeparam name="TElem">
        ///     The element returned by the enumerator.
        /// </typeparam>
        /// <returns>
        ///     An <see cref="IEnumerable"/> for <see cref="c"/>.
        /// </returns>
        public static IEnumerable<TElem> Enumerate<TColl, [AssociatedType] TState, [AssociatedType] TElem, implicit N, implicit T>(TColl c)
            where N : CEnumerable<TColl, TState>
            where T : CEnumerator<TState, TElem> => new EnumerableShim<TColl>(c);

        public static void Foreach<TColl, [AssociatedType] TState, [AssociatedType] TElem, implicit N, implicit T>(TColl c, Action<TElem> f)
            where N : CEnumerable<TColl, TState>
            where T : CEnumerator<TState, TElem>
        {
            var state = N.GetEnumerator(c);
            while (true)
            {
                if (!T.MoveNext(ref state)) return;
                f(T.Current(ref state));
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
    ///     Instance of <see cref="CIndexable"/> for strings, using
    ///     character indexing.
    /// </summary>
    public instance CIndexableString : CIndexable<string, int, char>
    {
        char At(this string container, int i) => container[i];
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for bit arrays, using
    ///     bitwise indexing.
    /// </summary>
    public instance CIndexableBitArray : CIndexable<BitArray, int, bool>
    {
        bool At(this BitArray container, int i) => container[i];
    }

    /// <summary>
    ///     Instance of <see cref="CIndexable"/> for ranges, calculating the
    ///     indexed term in the bounded arithmetic sequence.
    /// </summary>
    public instance CIndexableRange : CIndexable<Range, int, int>
    {
        int At(this Range range, int n) => range.start + (range.step * n);
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
    public instance CIndexableZip2<A, [AssociatedType] AE, B, [AssociatedType] BE, [AssociatedType]Ix, implicit IA, implicit IB> : CIndexable<(A, B), Ix, (AE, BE)>
        where IA : CIndexable<A, Ix, AE>
        where IB : CIndexable<B, Ix, BE>
    {
        (AE, BE) At(this (A, B) tup, Ix i) => (IA.At(tup.Item1, i), IB.At(tup.Item2, i));
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for strings, using string length.
    /// </summary>
    public instance StaticCountable_String : CStaticCountable<string>
    {
        int Count(this string container) => container.Length;
    }

    /// <summary>
    ///     Instance of <see cref="CLength"/> for bit arrays, using bit length.
    /// </summary>
    public instance StaticCountable_BitArray : CStaticCountable<BitArray>
    {
        int Count(this BitArray container) => container.Length;
    }

    /// <summary>
    ///     Static count for ranges, calculating the
    ///     number of terms in the bounded arithmetic sequence.
    /// </summary>
    public instance StaticCountable_Range : CStaticCountable<Range>
    {
        int Count(this Range range) => ((range.end - range.start) / range.step) + 1;
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
    public instance StaticCountable_Zip2<A, B, implicit LA, implicit LB> : CStaticCountable<(A, B)>
        where LA : CStaticCountable<A>
        where LB : CStaticCountable<B>
    {
        int Count(this (A, B) tup) => Math.Min(tup.Item1.Count(), tup.Item2.Count());
    }

    /// <summary>
    ///     Unspecialised implementation of CEnumerable based on CLength and
    ///     CIndexable, and perf benchmarks for it.
    /// </summary>
    static class Unspecialised
    {
        public static void RunWordTest(string[] words1, string[] words2, int[][] scores, int runs)
        {
            Enumerate("xyzzy");
            Enumerate(("abcdefghijklmnopqrstuvwxyz", new int[] { }));
            var wt = new WordTest(words1, words2, scores, runs);

            var cenumerableShimTotalTime = wt.RunCEnumerableShim();
            Console.Out.WriteLine($"TOTAL (CEnumerable Shim):     {cenumerableShimTotalTime}s");

            var cenumerableForeachTotalTime = wt.RunCEnumerableForeach();
            Console.Out.WriteLine($"TOTAL (CEnumerable Foreach):  {cenumerableForeachTotalTime}s");

            var cenumerableUnrolledTotalTime = wt.RunCEnumerableUnrolled();
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
                        var state1 = (words1, words2).GetEnumerator();
                        while (true)
                        {
                            if (!CEnumerator<IndexBoundCursor<(string[], string[]), int, (string, string)>>.MoveNext(ref state1)) break;
                            var tup1 = CEnumerator<IndexBoundCursor<(string[], string[]), int, (string, string)>>.Current(ref state1);

                            var ltotal = 0;
                            var rtotal = 0;

                            var state2 = CEnumerable<int[][], IndexBoundCursor<int[][], int, int[]>>.GetEnumerator(scores);
                            while (true)
                            {
                                if (!CEnumerator<IndexBoundCursor<int[][], int, int[]>>.MoveNext(ref state2)) break;
                                var score = CEnumerator<IndexBoundCursor<int[][], int, int[]>>.Current(ref state2);

                                var lcount = 0;
                                var rcount = 0;

                                var state3 = (("abcdefghijklmnopqrstuvwxyz", score)).GetEnumerator();
                                while (true)
                                {
                                    if (!CEnumerator<IndexBoundCursor<(string, int[]), int, (char, int)>>.MoveNext(ref state3)) break;
                                    var tup2 = CEnumerator<IndexBoundCursor<(string, int[]), int, (char, int)>>.Current(ref state3);

                                    var state4 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerator<IndexBoundCursor<string, int, char>>.MoveNext(ref state4)) break;
                                        var letter = CEnumerator<IndexBoundCursor<string, int, char>>.Current(ref state4);
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    }
                                    var state5 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerator<IndexBoundCursor<string, int, char>>.MoveNext(ref state5)) break;
                                        var letter = CEnumerator<IndexBoundCursor<string, int, char>>.Current(ref state5);
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
        public instance CEnumerableString : CEnumerable<string, (char[], int, char)>
        {
            (char[], int, char) GetEnumerator(this string str) => (str.ToCharArray(), -1, default(char));
        }
        public instance CEnumeratorString : CResettableEnumerator<(char[], int, char), char>
        {
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

        // use ArrayCursor for the specialised array enumerator.

        /// <summary>
        ///     Instance of <see cref="CEnumerable"/> for zipping a tuple of
        ///     enumerables into an enumerable of tuples.
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
        public instance CEnumeratorZip2<AS, [AssociatedType] AE,
                                        BS, [AssociatedType] BE,
                                        implicit EA, implicit EB>
                                        : CEnumerator<(AS, BS), (AE, BE)>
            where EA : CEnumerator<AS, AE>
            where EB : CEnumerator<BS, BE>
        {
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

        /// <summary>
        ///     Instance of <see cref="CEnumerable"/> for zipping a tuple of
        ///     enumerables into an enumerable of tuples.
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
        public instance CEnumerableZip2<A, [AssociatedType] AS,
                                        B, [AssociatedType] BS,
                                        implicit EA, implicit EB>
                                        : CEnumerable<(A, B), (AS, BS)>
            where EA : CEnumerable<A, AS>
            where EB : CEnumerable<B, BS>
        {
            (AS, BS) GetEnumerator((A, B) tup) =>
                (EA.GetEnumerator(tup.Item1), EB.GetEnumerator(tup.Item2));
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
                        var state1 = (words1, words2).GetEnumerator();
                        while (true)
                        {
                            if (!CEnumerator<(ArrayCursor<string>, ArrayCursor<string>)>.MoveNext(ref state1)) break;
                            var tup1 = CEnumerator<(ArrayCursor<string>, ArrayCursor<string>)>.Current(ref state1);

                            var ltotal = 0;
                            var rtotal = 0;

                            var state2 = CEnumerable<int[][]>.GetEnumerator(scores);
                            while (true)
                            {
                                if (!CEnumerator<ArrayCursor<int[]>>.MoveNext(ref state2)) break;
                                var score = CEnumerator<ArrayCursor<int[]>>.Current(ref state2);

                                var lcount = 0;
                                var rcount = 0;

                                var state3 = ("abcdefghijklmnopqrstuvwxyz", score).GetEnumerator();
                                while (true)
                                {
                                    if (!CEnumerator<((char[], int, char), ArrayCursor<int>)>.MoveNext(ref state3)) break;
                                    var tup2 = CEnumerator<((char[], int, char), ArrayCursor<int>)>.Current(ref state3);

                                    var state4 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerator<(char[], int, char)>.MoveNext(ref state4)) break;
                                        var letter = CEnumerator<(char[], int, char)>.Current(ref state4);
                                        if (letter == tup2.Item1) lcount += tup2.Item2;
                                    }
                                    var state5 = CEnumerable<string>.GetEnumerator(tup1.Item1);
                                    while (true)
                                    {
                                        if (!CEnumerator<(char[], int, char)>.MoveNext(ref state5)) break;
                                        var letter = CEnumerator<(char[], int, char)>.Current(ref state5);
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
    class EnumeratorShim<S, E, implicit N> : IEnumerator<E>
        where N : CEnumerator<S, E>
    {
        private S _state;

        /// <summary>
        ///     Creates an enumerator shim.
        /// </summary>
        /// <param name="state">
        ///     The collection shim to erase.
        /// </param>
        public EnumeratorShim(S state)
        {
            _state = state;
        }

        public E Current => N.Current(ref _state);
        object IEnumerator.Current => N.Current(ref _state);
        public bool MoveNext() => N.MoveNext(ref _state);
        public void Reset() { throw new NotSupportedException("can't reset a non-resettable CEnumerator"); }
        void IDisposable.Dispose() { N.Dispose(ref _state); }
    }

    /// <summary>
    ///     Adaptor converting <see cref="CEnumerable"/> into
    ///     <see cref="IEnumerable"/>.
    /// </summary>
    /// <typeparam name="C">
    ///     The type to be enumerated.
    /// </typeparam>
    /// <typeparam name="S">
    ///     The state held by the enumerator.
    /// </typeparam>
    /// <typeparam name="E">
    ///     The element returned by the enumerator.
    /// </typeparam>
    class EnumerableShim<C, [AssociatedType] S, [AssociatedType] E, implicit N, implicit T> : IEnumerable<E>
        where N : CEnumerable<C, S>
        where T : CEnumerator<S, E>
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

        public IEnumerator<E> GetEnumerator() => new EnumeratorShim<S, E>(_collection.GetEnumerator());
        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorShim<S, E>(_collection.GetEnumerator());
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
