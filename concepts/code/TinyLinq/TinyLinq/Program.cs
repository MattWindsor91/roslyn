using System;
using System.Concepts;
using System.Concepts.Prelude;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Attributes.Exporters;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq
{
    [CsvExporter, HtmlExporter, MarkdownExporter, RPlotExporter]
    [LegacyJitX86Job, RyuJitX64Job]
    public class WarrenSumBenchmarks
    {
        public Func<int, bool> pred = item => item % 10 == 0;
        public Func<int, int> proj = item => item + 5;

        // Original source:
        // https://gist.github.com/mattwarren/e528bc7c43864baad93ff33eb038005b

        private static readonly int[] items = Enumerable.Range(0, 1000).ToArray();

        [Benchmark(Baseline = true, Description = "Iterative")]
        public int Iterative()
        {
            var counter = 0;
            foreach (var item in items)
            {
                if (item % 10 == 0) counter += item + 5;
            }
            return counter;
        }

        [Benchmark(Description = "Iterative (boxed)")]
        public int Iterative_Funcs()
        {
            var counter = 0;
            foreach (var item in items)
            {
                if (pred(item)) counter += proj(item);
            }
            return counter;
        }

        [Benchmark(Description = "LINQ")]
        public int Linq()
        {
            var results = items.Where(i => i % 10 == 0).Select(i => i + 5);
            var counter = 0;
            foreach (var result in results)
            {
                counter += result;
            }

            return counter;
        }

        [Benchmark(Description = "TinyLINQ (Unspec)")]
        public int TinyLinq_Unspecialised()
        {
            var results =
                items
                .CWhere<int[], int, Where<ArrayCursor<int>, int>, Where_Enumerable<int[], int, ArrayCursor<int>, Enumerable_Array<int>>>((int i) => i % 10 == 0)
                .CSelect((int i) => i + 5);
            var counter = 0;
            // TODO: work out why this inference is failing.
            while (CEnumerator<SelectOfWhere<ArrayCursor<int>, int, int>>.MoveNext(ref results))
            {
                counter += CEnumerator<SelectOfWhere<ArrayCursor<int>, int, int>>.Current(ref results);
            }

            return counter;
        }

        [Benchmark(Description = "TinyLINQ (Spec)")]
        public int TinyLinq()
        {
            var results = items.CWhere((int i) => i % 10 == 0).CSelect((int i) => i + 5);
            var counter = 0;
            // TODO: work out why this inference is failing.
            while (CEnumerator<ArraySelectOfWhere<int, int>, int>.MoveNext(ref results))
            {
                counter += CEnumerator<ArraySelectOfWhere<int, int>, int>.Current(ref results);
            }

            return counter;
        }

        [Benchmark(Description = "TinyLINQ (Sum)")]
        public int TinyLinq_Sum() =>
            items.CWhere((int i) => i % 10 == 0).CSelect((int i) => i + 5).CSum();
    }

    [CsvExporter, HtmlExporter, MarkdownExporter, RPlotExporter]
    [LegacyJitX86Job, RyuJitX64Job]
    public class WarrenCountBenchmarks
    {
        private static readonly int[] items = Enumerable.Range(0, 1000).ToArray();
        public Func<int, bool> pred = item => item % 10 == 0;

        [Benchmark(Baseline = true, Description = "Iterative")]
        public int Iterative()
        {
            var i = 0;
            foreach (var item in items)
            {
                if (item % 10 == 0)
                {
                    i++;
                }
            }
            return i;
        }

        [Benchmark(Description = "Iterative (boxed)")]
        public int Iterative_Funcs()
        {
            var i = 0;
            foreach (var item in items)
            {
                if (pred(i))
                {
                    i++;
                }
            }
            return i;
        }

        [Benchmark(Description = "LINQ")]
        public int Linq()
        {
            return items.Where(i => i % 10 == 0).Count();
        }

        [Benchmark(Description = "TinyLINQ (Unspec)")]
        public int TinyLinq_Unspecialised()
        {
            return
                items
                .CWhere<int[], int, Where<ArrayCursor<int>, int>, Where_Enumerable<int[], int, ArrayCursor<int>, Enumerable_Array<int>>>((int i) => i % 10 == 0)
                .CCount();
        }

        [Benchmark(Description = "TinyLINQ (Spec)")]
        public int TinyLinq()
        {
            return items.CWhere((int i) => i % 10 == 0).CCount();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<WarrenSumBenchmarks>();
            BenchmarkRunner.Run<WarrenCountBenchmarks>();    
        }
    }
}
   
