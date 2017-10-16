---
title: 'Concept-C#'
subtitle: 'Progress with TinyLINQ'
author:
  - Claudio Russo
  - Matt Windsor
date: Monday 16 November 2017
---
# TinyLINQ

- Implementation of LINQ to Objects in Concept-C#
- Why?
  - Reduce virtualisation
  - Open up for better specialisation
  - Demonstrate a nontrivial application of concepts

# Benchmarks

Modified version of the [LinqOptimizer][] benchmarks library.

Each benchmark compares:

- Normal .NET Framework 4.7 LINQ
- TinyLINQ with no data structure specialisation ('U' TinyLINQ)
- TinyLINQ with array data structure specialisation and fusion ('S' TinyLINQ)
- LinqOptimizer (aggressive query expression optimisation)

[LinqOptimizer]: http://nessos.github.io/LinqOptimizer/

# Select

In normal _LINQ to Objects_, this is:

    public static partial class Enumerable
    {
        IEnumerable<TResult> Select(this IEnumerable<TSource> source,
                                    Func<TSource, TResult> selector)
        { /* ... */ }
    }

The concept version is:

    public concept CSelect<TSourceColl, TSource, TResult,
                           [AssociatedType]TResultColl>
    {
        TResultColl Select(this TSourceColl source,
                           Func<TSource, TResult> selector);
    }

This encoding lets us specialise according to the collection we're given by
varying `TResultColl`

# Select: Unspecialised Instance (UTL)

Create a struct to hold the state of the enumeration:

    public struct Select<TSourceEnum, TSource, TResult>
    {
        public TSourceEnum source;
        public Func<TSource, TResult> selector;
        public TResult current;
    }

And an instance to return `Select`s for any enumerator:

    public instance Select_Enumerator<TSourceEnum, TSource, TResult, implicit E>
        : CSelect<TSourceEnum, Select<TSourceEnum, TSource, TResult>>
        where E : CEnumerator<TSourceEnum, TSource>
    {
        Select<TSourceEnum, TSource, TResult> Select(
            this TSourceEnum source,
            Func<TSource, TResult> selector) 
        {
            return new Select<TSourceEnum, TSource, TResult>
            {
                source = source,
                selector = selector
            };
        }
    }

Not seen: glue instance to lift this to `CEnumerable`

# Select: Specialised Instance (STL)

Hand-inline the array enumeration:

    public struct ArraySelect<TSource, TResult>
    {
        public TSource[] source;
        public Func<TSource, TResult> selector;
        public int index, length;
        public TResult current;
    }

And make a new, more specialised instance:

    public instance Select_ArrayCursor<TSource, TResult>
        : CSelect<ArrayCursor<TSource>, TSource, TResult,
                  ArraySelect<TSource, TResult>>
    {
        ArraySelect<TSource, TResult> Select(this ArrayCursor<TSource> source,
                                             Func<TSource, TResult> selector)
        {
            return new ArraySelect<TSource, TResult>
            {
                source = source.array, // underlying array
                selector = selector,
                index = -1, length = source.array.Length
            };
        }
    }

where `ArrayCursor` is the enumerator struct we use for `CEnumerator` on arrays
(similar to `List.Enumerator`)

**Known issue**: should really take `ArrayCursor`'s current index

# Select Benchmark

Sum of squares:

    return values.Select(x => x * x).Sum();

where

    const int useSameSeedEveryTimeToHaveSameData = 08041988;
    protected Random rnd = new Random(useSameSeedEveryTimeToHaveSameData);
    values = Enumerable.Range(1, 1000000).Select(x => rnd.NextDouble()).ToArray();

 |          Method |                Mean |              Error |           Std. Dev. |              Median |       Scaled |    Scaled SD |      Gen 0 |     Allocated |
 |---------------- |--------------------:|-------------------:|--------------------:|--------------------:|-------------:|-------------:|-----------:|--------------:|
 | LINQ (baseline) |     6,973,307.75 ns |        376.9286 ns |         314.7524 ns |     6,973,387.06 ns |         1.00 |         0.00 |          - |         128 B |
 | U. TinyLINQ     |     7,944,699.07 ns |     19,191.1397 ns |      17,951.4022 ns |     7,938,954.14 ns |         1.14 |         0.00 |          - |           0 B |
 | **S. TinyLINQ** | **4,573,600.18 ns** | **88,844.7148 ns** | **127,418.4352 ns** | **4,498,185.13 ns** |     **0.66** |     **0.02** |      **-** |       **0 B** |
 | LINQOptimizer   |       942,023.65 ns |      9,273.0175 ns |       7,743.3889 ns |       940,342.03 ns |         0.14 |         0.00 |     3.9063 |       20880 B |

Specialised: One-third reduction in time compared to LINQ.
Unspecialised: slight slowdown, likely due to enumerator call overhead.

# SelectMany



# Complex Queries

# Sum (Vanilla Linq)

Linq's ``Sum`` methods are hand-specialized to some (but not all) numeric types. 
Same is true for ``Max``, ``Min``, ``Average`` etc.

        public static int Sum(this IEnumerable<int> source)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            int sum = 0;
            checked {
              foreach (int v in source)
                sum += v;
            }
            return sum;
        }
        public static int? Sum(this IEnumerable<int?> source) {...ditto...}
        public static long Sum(this IEnumerable<long> source) {...ditto...}
        public static long? Sum(this IEnumerable<long?> source)  {...ditto...}
        public static float Sum(this IEnumerable<float> source)  {...ditto...}
        public static float? Sum(this IEnumerable<float?> source)  {...ditto...}
        public static double Sum(this IEnumerable<double> source)  {...ditto...}
        public static double? Sum(this IEnumerable<double?> source)  {...ditto...}
        public static decimal Sum(this IEnumerable<decimal> source)  {...ditto...}
        public static decimal? Sum(this IEnumerable<decimal?> source)  {...ditto...}
       
(iteration (via ``foreach``) is unspecialized!)

# Sum (Concept Linq)

Concept Linq has *one* generic implementation of ``Sum``, abstracted on all current (and future) numeric instances:

    public concept CSum<TEnum, [AssociatedType] TElem>
    {
        TElem Sum(this TEnum e);        
    }
    
    public instance Sum_Enumerable_Num<TColl, [AssociatedType] TEnum, [AssociatedType] TElem, implicit E, implicit N> : CSum<TColl, TElem>
        where E : CEnumerable<TColl, TEnum, TElem>
        where N : Num<TElem>
    {
        TElem Sum(this TColl c)
        {
            var e = E.GetEnumerator(c);
            var sum = N.FromInteger(0);
            var count = 0;
            E.Reset(ref e);
            while (E.MoveNext(ref e))
            {
                count++;
                sum += E.Current(ref e);
            }
            return sum;
        }
    }

Moreover, iteration (via CEnumerable<TColl,TENum,Elem>) can be specialized!

# Caveats

- TinyLINQ plays hard and fast with enumerators
  - Lots of assumptions that enumerators are fresh and won't be re-used
  - Thread-unsafe
  - Very little argument checking
  - Working to clean up the concepts enumerator library to mitigate some of
    these problems
- No rigorous testing (yet) of the results coming out
  - We use our `SerialPBT` property based testing library to test certain
    queries, but this only tests small input spaces
  - Next step: unit tests based on the actual benchmarks

## Conclusions

- Promising results for simple queries
  - Faster than LINQ when matching or surpassing corefx's level of 
- Unclear why complex queries become slower
  - further investigation required