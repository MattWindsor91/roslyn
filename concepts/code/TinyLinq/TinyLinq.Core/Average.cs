using System.Concepts;
using System.Concepts.Enumerable;
using System.Concepts.Prelude;

namespace TinyLinq
{
    /// <summary>
    /// Concept for averaging over an enumerator.
    /// </summary>
    public concept CAverage<TEnum, [AssociatedType] TElem>
    {
        /// <summary>
        /// Averages over all of the elements of an enumerator.
        /// </summary>
        /// <param name="e">
        /// The enumerator to average.
        /// </param>
        /// <returns>
        /// The average of all elements reachable by the enumerator.
        /// </returns>
        TElem Average(this TEnum e);        
    }

    /// <summary>
    /// If we can average over an enumerator, we can average over its enumerable.
    /// </summary>
    [Overlappable]
    public instance Average_Enumerable<TColl, [AssociatedType]TEnum, [AssociatedType]TDst, implicit E, implicit A> : CAverage<TColl, TDst>
        where E : CEnumerable<TColl, TEnum>
        where A : CAverage<TEnum, TDst>
    {
        TDst Average(this TColl c) => c.GetEnumerator().Average();
    }

    /// <summary>
    /// Averaging over a general enumerator of integers, promoting the
    /// result to a double.
    /// </summary>
    public instance Average_Enumerator_Int<TEnum, implicit E> : CAverage<TEnum, double>
        where E : CEnumerator<TEnum, int>
    {
        double Average(this TEnum e)
        {
            var sum = 0;
            var count = 0;

            while (E.MoveNext(ref e))
            {
                count++;
                sum += E.Current(ref e);
            }

            return (double)sum / count;
        }
    }

    /// <summary>
    /// Averaging over a general enumerator, when the element is fractional.
    /// </summary>
    public instance Average_Enumerator_Fractional<TEnum, [AssociatedType] TElem, implicit E, implicit F> : CAverage<TEnum, TElem>
        where E : CEnumerator<TEnum, TElem>
        where F : Fractional<TElem>
    {
        TElem Average(this TEnum e)
        {
            var sum = F.FromInteger(0);
            var count = 0;

            while (E.MoveNext(ref e))
            {
                count++;
                sum += E.Current(ref e);
            }

            return sum / F.FromInteger(count);
        }
    }

    /// <summary>
    /// Summation over a general enumerator, when the element is a monoid.
    /// Summation is iterated monoid append with monoid empty as a unit.
    /// </summary>
    public instance Average_Enumerator_Monoid<TEnum, [AssociatedType] TElem, implicit E, implicit F> : CAverage<TEnum, TElem>
        where E : CEnumerator<TEnum, TElem>
        where F : Fractional<TElem>
    {
        TElem Average(this TEnum e)
        {
            var sum = F.FromInteger(0);
            var count = 0;

            while (E.MoveNext(ref e))
            {
                count++;
                sum += E.Current(ref e);
            }

            return sum / F.FromInteger(count);
        }
    }
}
