using System.Concepts;
using System.Concepts.Enumerable;
using System.Concepts.Prelude;

namespace TinyLinq
{
    /// <summary>
    /// Concept for averaging over an enumerator.
    /// </summary>
    public concept CAverage<TSourceColl, [AssociatedType] TElem>
    {
        /// <summary>
        /// Averages over all of the elements of an enumerable.
        /// </summary>
        /// <param name="source">
        /// The enumerable to average.
        /// </param>
        /// <returns>
        /// The average of all elements in the enumerable.
        /// </returns>
        TElem Average(this TSourceColl source);        
    }

    /// <summary>
    /// Averaging over a general enumerator of integers, promoting the
    /// result to a double.
    /// </summary>
    public instance Average_Enumerator_Int<TSourceColl, [AssociatedType]TSourceEnum, implicit Eb, implicit Et> : CAverage<TSourceColl, double>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, int>
    {
        double Average(this TSourceColl source)
        {
            var sum = 0;
            var count = 0;

            var e = source.GetEnumerator();
            while (Et.MoveNext(ref e))
            {
                count++;
                sum += Et.Current(ref e);
            }

            return (double)sum / count;
        }
    }

    /// <summary>
    /// Averaging over a general enumerator, when the element is fractional.
    /// </summary>
    [Overlappable]
    public instance Average_Enumerator_Fractional<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType] TSource, implicit Eb, implicit Et, implicit F> : CAverage<TSourceColl, TSource>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
        where F : Fractional<TSource>
    {
        TSource Average(this TSourceColl source)
        {
            var sum = F.FromInteger(0);
            var count = 0;

            var e = source.GetEnumerator();
            while (Et.MoveNext(ref e))
            {
                count++;
                sum += Et.Current(ref e);
            }

            return sum / F.FromInteger(count);
        }
    }
}
