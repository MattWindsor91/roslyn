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

# Sum: Vanilla LINQ

LINQ's ``Sum`` methods are hand-specialized to some (but not all) numeric types. 
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

# Sum: TinyLINQ

TinyLINQ has *one* generic implementation of ``Sum``, abstracted on all current (and future) numeric instances:

    public concept CSum<TEnum, [AssociatedType] TElem>
    {
        TElem Sum(this TEnum source);
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
            E.Reset(ref e);  // FIXME: remove this
            while (E.MoveNext(ref e))
            {
                count++;
                sum += E.Current(ref e);
            }
            return sum;
        }
    }

Moreover, iteration (via CEnumerable<TColl,TENum,Elem>) can be specialized!

# Sum Benchmarks

    return values.Sum();

where

    const int useSameSeedEveryTimeToHaveSameData = 08041988;
    protected Random rnd = new Random(useSameSeedEveryTimeToHaveSameData);
    values = Enumerable.Range(1, 1000000).Select(x => rnd.NextDouble()).ToArray();

| Method          |        Mean (ns) |   Error (ns) |  StdDev (ns) |      Median (ns) |   Scaled |  Gen 0 | Allocated |
|-----------------|-----------------:|-------------:|-------------:|-----------------:|---------:|-------:|----------:|
| LINQ (baseline) |     5,033,907.45 |   8,971.8780 |   7,491.9237 |     5,032,082.93 |     1.00 |      - |      64 B |
| U. TinyLINQ     |     2,189,911.82 |     244.1549 |     190.6201 |     2,189,913.06 |     0.44 |      - |       0 B |
| **S. TinyLINQ** | **2,189,101.51** | **530.0816** | **413.8528** | **2,189,012.44** | **0.43** |  **-** |   **0 B** |
| LINQOptimizer   |       880,015.47 |     371.2546 |     289.8511 |       879,941.68 |     0.17 | 2.9297 |   18920 B |

(U. TinyLINQ and S. TinyLINQ run the same code here)

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

| Method          |        Mean (ns) |      Error (ns) |      StdDev (ns) |      Median (ns) |   Scaled |  Gen 0 | Allocated |
|-----------------|-----------------:|----------------:|-----------------:|-----------------:|---------:|-------:|----------:|
| LINQ (baseline) |     6,973,307.75 |        376.9286 |         314.7524 |     6,973,387.06 |     1.00 |      - |     128 B |
| U. TinyLINQ     |     7,944,699.07 |     19,191.1397 |      17,951.4022 |     7,938,954.14 |     1.14 |      - |       0 B |
| **S. TinyLINQ** | **4,573,600.18** | **88,844.7148** | **127,418.4352** | **4,498,185.13** | **0.66** |  **-** |   **0 B** |
| LINQOptimizer   |       942,023.65 |      9,273.0175 |       7,743.3889 |       940,342.03 |     0.14 | 3.9063 |   20880 B |

Specialised: One-third reduction in time compared to LINQ.
Unspecialised: slight slowdown, likely due to enumerator call overhead.

# SelectMany

Normally, SelectMany looks like:

    public static IEnumerable<TResult>
    SelectMany<TSource, TCollection, TResult>(this IEnumerable<TSource> source,
                                              Func<TSource, IEnumerable<TCollection>> collectionSelector,
                                              Func<TSource, TCollection, TResult> resultSelector)

Ours is:

    public concept CSelectMany<TElemColl, [AssociatedType] TElem,
                               TCollectionColl, [AssociatedType] TCollection,
                               TResult, [AssociatedType] TResultColl>
    {
        TResultColl SelectMany(this TElemColl src,
                               Func<TElem, TCollectionColl> selector,
                               Func<TElem, TCollection, TResult> resultSelector);
    }

Generic soup?

# SelectMany Benchmark

    return (from x in dim1
            from y in dim2
            select x * y).Sum();

where

    const int useSameSeedEveryTimeToHaveSameData = 08041988;
    protected Random rnd = new Random(useSameSeedEveryTimeToHaveSameData);
    values = Enumerable.Range(1, 1000000).Select(x => rnd.NextDouble()).ToArray();
    dim1 = values.Take(values.Length / 10).ToArray();
    dim2 = values.Take(20).ToArray();

| Method          |         Mean (ns) |       Error (ns) |      StdDev (ns) |       Median (ns) |       Scaled |        Gen 0 |     Allocated |
|-----------------|------------------:|-----------------:|-----------------:|------------------:|-------------:|-------------:|--------------:|
| LINQ (baseline) |     28,007,892.30 |      48,721.2904 |      40,684.4801 |     28,006,118.71 |         1.00 |     593.7500 |     3200289 B |
| U. TinyLINQ     |     20,427,620.20 |     408,539.5068 |     501,723.2035 |     20,625,689.87 |         0.73 |            - |         256 B |
| **S. TinyLINQ** | **10,931,075.23** |  **11,392.2479** |  **10,098.9300** | **10,927,728.40** |     **0.39** |        **-** |     **128 B** |
| LINQOptimizer   |      1,813,231.38 |      14,839.7519 |      12,391.8637 |      1,807,227.35 |         0.06 |       5.8594 |       39713 B |

LINQ doesn't do any optimisation here, so the more fair comparison is to
U. TinyLINQ.

# Obligatory bad benchmark

    return (from a in Enumerable.Range(1, 1000 + 1)
            from b in Enumerable.Range(a, 1000 + 1 - a)
            from c in Enumerable.Range(b, 1000 + 1 - b)
            where a * a + b * b == c * c
            select true).Count();

For S. TinyLINQ, we use a struct rewriting of `Range`.

 |         Method  |                   Mean |                  Error |                 StdDev |                 Median |       Scaled |     ScaledSD |
 |-----------------|-----------------------:|-----------------------:|-----------------------:|-----------------------:|-------------:|-------------:|
 | **S. TinyLINQ** | **4,966,219,778.7 ns** | **11,769,027.1854 ns** | **10,432,935.0296 ns** | **4,963,239,193.8 ns** |     **1.40** |     **0.00** |
 | U. TinyLINQ     |     3,916,384,212.6 ns |      5,913,565.2231 ns |      5,531,552.0405 ns |     3,914,310,472.2 ns |         1.10 |         0.00 |
 | LINQ (baseline) |     3,553,785,148.6 ns |      4,300,104.4210 ns |      4,022,319.9520 ns |     3,552,339,246.8 ns |         1.00 |         0.00 |
 | LINQOptimizer   |       167,583,382.8 ns |      1,571,614.0967 ns |      1,393,194.8243 ns |       166,821,341.4 ns |         0.05 |         0.00 |

Not sure why we get this upside-down benchmark...!

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
  - Quick validation of the final results used in the benchmarks

## Conclusions

- Promising results for simple queries
  - Faster than LINQ when matching or surpassing corefx's level of 
- Unclear why complex queries become slower
  - further investigation required