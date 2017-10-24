using System;
using System.Concepts;
using System.Concepts.Countable;
using System.Concepts.Prelude;
using System.Concepts.Enumerable;
using static System.Concepts.Enumerable.Instances;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Attributes.Exporters;
using TinyLinq.SpecialisedInstances;

namespace TinyLinq.Bench
{
    [MemoryDiagnoser]
    [CsvExporter, HtmlExporter, MarkdownExporter]
    public abstract class BenchmarksBase
    { }

    public abstract class WarrenBenchmarksBase : BenchmarksBase
    {
        public abstract int Iterative();

        /// <summary>
        /// Sanity-check the various benchmark methods.
        /// </summary>
        /// <returns>
        /// True if, and only if, each benchmark method returns the same as
        /// the LINQ version.
        /// </returns>
        public virtual bool SanityCheck()
        {
            var oracle = Iterative();
            var members =
                GetType().FindMembers(
                    System.Reflection.MemberTypes.Method,
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly,
                    new System.Reflection.MemberFilter(
                        (mi, _) => mi.Name != nameof(Iterative)
                    ),
                    null);
            foreach (var m in members)
            {
                System.Console.Write($"Sanity checking {m.Name}");
                var result = (m as System.Reflection.MethodInfo)?.Invoke(this, null) as int?;
                System.Console.WriteLine($": {result}");
                if (!(result?.Equals(oracle)) ?? false)
                {
                    System.Console.WriteLine("Failed!");
                    return false;
                }
            }
            return true;
        }
    }

    public class WarrenSumBenchmarks : WarrenBenchmarksBase
    {
        public Func<int, bool> pred = item => item % 10 == 0;
        public Func<int, int> proj = item => item + 5;

        // Original source:
        // https://gist.github.com/mattwarren/e528bc7c43864baad93ff33eb038005b

        private static readonly int[] items = Enumerable.Range(0, 1000).ToArray();

        [Benchmark(Baseline = true, Description = "Iterative")]
        public override int Iterative()
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
            var results = items.Where<int>(i => i % 10 == 0).Select<int, int>(i => i + 5);
            var counter = 0;
            foreach (var result in results)
            {
                counter += result;
            }

            return counter;
        }

        /*
        [Benchmark(Description = "TinyLINQ (Unspec)")]
        public int TinyLinq_Unspecialised()
        {
            var results =
                items
                .Where<int[], int, Where<ArrayCursor<int>, int>, Where_Enumerable<int[], ArrayCursor<int>, int, Where<ArrayCursor<int>, int>, Where_Enumerator<ArrayCursor<int>>, Enumerable_Array<int>>>((int i) => i % 10 == 0)
                .Select((int i) => i + 5);
            var counter = 0;
            // TODO: work out why this inference is failing.
            while (CEnumerator<SelectOfWhere<ArrayCursor<int>, int, int>>.MoveNext(ref results))
            {
                counter += CEnumerator<SelectOfWhere<ArrayCursor<int>, int, int>>.Current(ref results);
            }

            return counter;
        }
        */

        [Benchmark(Description = "TinyLINQ (Spec)")]
        public int TinyLinq()
        {
            var results = items.Where(i => i % 10 == 0).Select(i => i + 5);
            var counter = 0;
            // TODO: work out why this inference is failing.
            while (CEnumerator<ArraySelectOfWhereCursor<int, int>, int>.MoveNext(ref results))
            {
                counter += CEnumerator<ArraySelectOfWhereCursor<int, int>, int>.Current(ref results);
            }

            return counter;
        }

        [Benchmark(Description = "TinyLINQ (Sum)")]
        public int TinyLinq_Sum() =>
            items.Where(i => i % 10 == 0).Select(i => i + 5).Sum();
    }
    public class WarrenCountBenchmarks : WarrenBenchmarksBase
    {
        private static readonly int[] items = Enumerable.Range(0, 1000).ToArray();
        public Func<int, bool> pred = item => item % 10 == 0;

        [Benchmark(Baseline = true, Description = "Iterative")]
        public override int Iterative()
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
                if (pred(item))
                {
                    i++;
                }
            }
            return i;
        }

        [Benchmark(Description = "LINQ")]
        public int Linq()
        {
            return items.Where<int>(i => i % 10 == 0).Count();
        }

        /*
        [Benchmark(Description = "TinyLINQ (Unspec)")]
        public int TinyLinq_Unspecialised()
        {
            return
                items
                .CWhere<int[], int, Where<ArrayCursor<int>, int>, Where_Enumerable<int[], ArrayCursor<int>, int, Where<ArrayCursor<int>, int>, Where_Enumerator<ArrayCursor<int>>, Enumerable_Array<int>>>((int i) => i % 10 == 0)
                .CCount();
        }
        */

        [Benchmark(Description = "TinyLINQ (Spec)")]
        public int TinyLinq()
        {
            return items.Where(i => i % 10 == 0).Count();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "pythagorean")
                {
                    if (!(new PythagoreanBenchmarks()).SanityCheck())
                    {
                        return;
                    }
                    BenchmarkRunner.Run<PythagoreanBenchmarks>();
                    return;
                }
                else if (args[0] == "pprofile")
                {
                    new PythagoreanBenchmarks() { max = 200 }.EnumerableRangeFused();
                    return;
                }
            }

            if (!new WarrenSumBenchmarks().SanityCheck())
            {
                return;
            }
            if (!new WarrenCountBenchmarks().SanityCheck())
            {
                return;
            }

            BenchmarkRunner.Run<WarrenSumBenchmarks>();
            BenchmarkRunner.Run<WarrenCountBenchmarks>();    
        }
    }
}
   
