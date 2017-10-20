using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Exporters;
using BenchmarkDotNet.Attributes.Jobs;


namespace TinyLinq.Bench
{
    namespace Linq
    {
        using System.Linq;

        public static class Bench
        {
            public static int Run(int max)
            {
                var query =
                    from a in Enumerable.Range(1, max + 1)
                    from b in Enumerable.Range(a, max + 1 - a)
                    from c in Enumerable.Range(b, max + 1 - b)
                    where a * a + b * b == c * c
                    select true;
                return query.Count();
            }
        }
    }

    namespace EnumerableRange
    {
        using System.Concepts.Countable;
        using System.Concepts.Enumerable;
        using static System.Concepts.Enumerable.Instances;
        using System.Concepts.Prelude;
        using TinyLinq;

        namespace Unspec
        {
            public static class Bench
            {
                public static int Run(int max)
                {
                    var query =
                        from a in System.Linq.Enumerable.Range(1, max + 1)
                        from b in System.Linq.Enumerable.Range(a, max + 1 - a)
                        from c in System.Linq.Enumerable.Range(b, max + 1 - b)
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
            }
        }

        namespace Fused
        {
            using TinyLinq.SpecialisedInstances;

            public static class Bench
            {
                public static int Run(int max)
                {
                    var query =
                        from a in System.Linq.Enumerable.Range(1, max + 1)
                        from b in System.Linq.Enumerable.Range(a, max + 1 - a)
                        from c in System.Linq.Enumerable.Range(b, max + 1 - b)
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
            }
        }
    }

    namespace StructRange
    {
        using System.Concepts;
        using System.Concepts.Countable;
        using System.Concepts.Enumerable;
        using static System.Concepts.Enumerable.Instances;
        using System.Concepts.Prelude;
        using TinyLinq;

        namespace Unspec
        {
            public static class Bench
            {
                public static int Run(int max)
                {
                    var query =
                        from a in new Range<int> { start = 1, count = max + 1 }
                        from b in new Range<int> { start = a, count = max + 1 - a }
                        from c in new Range<int> { start = b, count = max + 1 - b }
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
            }
        }

        namespace Fused
        {
            using TinyLinq.SpecialisedInstances;

            public static class Bench
            {
                public static int Run(int max)
                {
                    var query =
                        from a in new Range<int> { start = 1, count = max + 1 }
                        from b in new Range<int> { start = a, count = max + 1 - a }
                        from c in new Range<int> { start = b, count = max + 1 - b }
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
            }
        }
    }

    namespace FlatStructRange
    {
        using System.Concepts;
        using System.Concepts.Countable;
        using System.Concepts.Enumerable;
        using static System.Concepts.Enumerable.Instances;
        using System.Concepts.Prelude;
        using TinyLinq;

        /// <summary>
        /// A non-generic range.
        /// </summary>
        /// <typeparam name="TNum">
        /// Type of numbers in the range.
        /// </typeparam>
        public struct FlatRange
        {
            /// <summary>
            /// The start of the range.
            /// </summary>
            public int start;
            /// <summary>
            /// The number of items in the range.
            /// </summary>
            public int count;

            public int At(int pos) => start + pos;

            public int End() => start + count;

            public FlatRange(int start, int count)
            {
                this.start = start;
                this.count = count;
            }
        }
        public struct FlatRangeCursor
        {
            /// <summary>
            /// The range over which we are iterating.
            /// </summary>
            public FlatRange range;
            /// <summary>
            /// The cached end of the range.
            /// </summary>
            public int end;
            /// <summary>
            /// The current item in the range.
            /// </summary>
            public int current;
            /// <summary>
            /// Whether we are one below the first item in the range.
            /// </summary>
            public bool reset;
            /// <summary>
            /// Whether we are one after the last item in the range.
            /// </summary>
            public bool finished;
        }

        /// <summary>
        /// Various enumerator instances for flat ranges.
        /// </summary>
        public instance CopyEnumerator_FlatRange : CCloneableEnumerator<FlatRangeCursor, int>
        {
            // TODO: catch inverted ranges and overflows
            // TODO: better optimisation if range is empty

            // RangeCursor is a value type.
            FlatRangeCursor Clone(ref this FlatRangeCursor e) =>
                new FlatRangeCursor
                {
                    range = e.range,
                    end = e.end,
                    reset = true,
                    finished = false
                };

            void Reset(ref FlatRangeCursor e)
            {
                e.reset = true;
                e.finished = false;
            }
            bool MoveNext(ref FlatRangeCursor e)
            {
                if (e.finished)
                {
                    return false;
                }

                if (e.reset)
                {
                    e.current = e.range.start;
                    e.reset = false;
                }
                else
                {
                    e.current++;
                }

                e.finished = e.end == e.current;
                return !e.finished;
            }

            int Current(ref FlatRangeCursor e) => e.current;
            void Dispose(ref FlatRangeCursor e) { }
        }

        /// <summary>
        /// Various enumerator instances for ranges.
        /// </summary>
        public instance Enumerable_FlatRange : CEnumerable<FlatRange, FlatRangeCursor>
        {
            FlatRangeCursor GetEnumerator(this FlatRange range) =>
                new FlatRangeCursor { range = range, end = range.start + range.count, reset = true, finished = false };
        }

        namespace Unspec
        {
            public static class Bench
            {
                public static int Run(int max)
                {
                    var q =
                        from a in new FlatRange { start = 1, count = max + 1 }
                        from b in new FlatRange { start = a, count = max + 1 - a }
                        from c in new FlatRange { start = b, count = max + 1 - b }
                        select true;
                    var r = q.GetEnumerator();


                    var query =
                        from a in new FlatRange { start = 1, count = max + 1 }
                        from b in new FlatRange { start = a, count = max + 1 - a }
                        from c in new FlatRange { start = b, count = max + 1 - b }
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
                public static int RunCtor(int max)
                {
                    var query =
                        from a in new FlatRange(1, max + 1)
                        from b in new FlatRange(a, max + 1 - a)
                        from c in new FlatRange(b, max + 1 - b)
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
            }
        }

        namespace Fused
        {
            using TinyLinq.SpecialisedInstances;

            public static class Bench
            {
                public static int Run(int max)
                {
                    var query =
                        from a in new FlatRange { start = 1, count = max + 1 }
                        from b in new FlatRange { start = a, count = max + 1 - a }
                        from c in new FlatRange { start = b, count = max + 1 - b }
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
                public static int RunCtor(int max)
                {
                    var query =
                        from a in new FlatRange(1,  max + 1)
                        from b in new FlatRange(a, max + 1 - a)
                        from c in new FlatRange(b, max + 1 - b)
                        where a * a + b * b == c * c
                        select true;
                    return query.Count();
                }
            }
        }
    }

    /// <summary>
    /// Benchmark playpen for Pythagorean triples.
    /// </summary>
    [CsvExporter, HtmlExporter, MarkdownExporter, RPlotExporter]
    public class PythagoreanBenchmarks
    {
        /// <summary>
        /// Sanity-check the various benchmark methods.
        /// </summary>
        /// <returns>
        /// True if, and only if, each benchmark method returns the same as
        /// the LINQ version.
        /// </returns>
        public static bool SanityCheck()
        {
            var p = new PythagoreanBenchmarks();
            var oracle = p.LinqBench();
            var members =
                typeof(PythagoreanBenchmarks).FindMembers(
                    System.Reflection.MemberTypes.Method,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly,
                    new System.Reflection.MemberFilter(
                        (mi, _) => mi.Name != "LinqBench"
                    ),
                    null);
            foreach (var m in members)
            {
                System.Console.Write($"Sanity checking {m.Name}");
                var result = (m as System.Reflection.MethodInfo)?.Invoke(p, null) as int?;
                System.Console.WriteLine($": {result}");
                if (!(result?.Equals(oracle)) ?? false)
                {
                    System.Console.WriteLine("Failed!");
                    return false;
                }
            }
            return true;
        }

        public int max = 100;

        [Benchmark(Baseline = true)]
        public int LinqBench() => Linq.Bench.Run(max);

        [Benchmark]
        public int EnumerableRangeUnspec() => EnumerableRange.Unspec.Bench.Run(max);

        [Benchmark]
        public int EnumerableRangeFused() => EnumerableRange.Fused.Bench.Run(max);

        [Benchmark]
        public int StructRangeUnspec() => StructRange.Unspec.Bench.Run(max);

        [Benchmark]
        public int StructRangeFused() => StructRange.Fused.Bench.Run(max);

        [Benchmark]
        public int FlatStructRangeUnspec() => FlatStructRange.Unspec.Bench.Run(max);

        [Benchmark]
        public int FlatStructRangeFused() => FlatStructRange.Fused.Bench.Run(max);

        [Benchmark]
        public int FlatStructRangeUnspecCtor() => FlatStructRange.Unspec.Bench.RunCtor(max);

        [Benchmark]
        public int FlatStructRangeFusedCtor() => FlatStructRange.Fused.Bench.RunCtor(max);
    }
}
