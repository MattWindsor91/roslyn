using System;
using System.Concepts;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Attributes.Exporters;

namespace TinyLinq
{
    [CsvExporter, HtmlExporter, MarkdownExporter, RPlotExporter]
    [LegacyJitX86Job, RyuJitX64Job]
    public class Benchmarks
    {
        public Func<int, bool> pred = item => item % 10 == 0;
        public Func<int, int> proj = item => item + 5;

        // Original source:
        // https://gist.github.com/mattwarren/e528bc7c43864baad93ff33eb038005b

        private static readonly int[] items = Enumerable.Range(0, 1000).ToArray();

        [Benchmark(Baseline = true)]
        public int Iterative()
        {
            var counter = 0;
            foreach (var item in items)
            {
                if (item % 10 == 0) counter += item + 5;
            }
            return counter;
        }

        [Benchmark]
        public int Iterative_Funcs()
        {
            var counter = 0;
            foreach (var item in items)
            {
                if (pred(item)) counter += proj(item);
            }
            return counter;
        }

        [Benchmark]
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


        [Benchmark]
        public int TinyLinq_Unspecialised()
        {
            var results =
                items
                .CWhere<int, int[], Where<ArrayCursor<int>, int>, Where_Enumerable<int, int[], ArrayCursor<int>, Enumerable_Array<int>>>((int i) => i % 10 == 0)
                .CSelect((int i) => i + 5);
            var counter = 0;
            // TODO: work out why this inference is failing.
            while (CEnumerator<int, SelectOfWhere<ArrayCursor<int>, int, int>>.MoveNext(ref results))
            {
                counter += CEnumerator<int, SelectOfWhere<ArrayCursor<int>, int, int>>.Current(ref results);
            }

            return counter;
        }


        [Benchmark]
        public int TinyLinq()
        {
            var results = items.CWhere((int i) => i % 10 == 0).CSelect((int i) => i + 5);
            var counter = 0;
            // TODO: work out why this inference is failing.
            while (CEnumerator<int, ArraySelectOfWhere<int, int>>.MoveNext(ref results))
            {
                counter += CEnumerator<int, ArraySelectOfWhere<int, int>>.Current(ref results);
            }

            return counter;
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Benchmarks b = new Benchmarks();
            Console.WriteLine(b.Iterative());
            Console.WriteLine(b.Linq());
            Console.WriteLine(b.TinyLinq());
            /*System.Diagnostics.Debugger.Launch();
            var b = new Benchmarks();
            for (var i = 0; i < 100_000; i++)
            {
                b.TinyLinq();
            }

            
            LinqSyntaxTests.Run();
            DesugaredTests.Run();*/
            BenchmarkRunner.Run<Benchmarks>();
            
        }
    }
}
   
