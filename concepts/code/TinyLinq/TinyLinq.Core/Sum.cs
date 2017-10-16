using System.Concepts;
using System.Concepts.Enumerable;
using System.Concepts.Monoid;
using System.Concepts.Prelude;

namespace TinyLinq
{
    /// <summary>
    /// Concept for summing over an enumerator.
    /// </summary>
    public concept CSum<TEnum, [AssociatedType] TElem>
    {
        /// <summary>
        /// Sums over all of the elements of an enumerator.
        /// </summary>
        /// <param name="e">
        /// The enumerator to sum.
        /// </param>
        /// <returns>
        /// The sum of all elements reachable by the enumerator.
        /// </returns>
        TElem Sum(this TEnum e);        
    }

    /// <summary>
    /// If we can sum over an enumerator, we can average over its enumerable.
    /// </summary>
    [Overlappable]
    public instance Sum_Enumerable<TColl, [AssociatedType]TEnum, [AssociatedType]TDst, implicit E, implicit S> : CSum<TColl, TDst>
        where E : CEnumerable<TColl, TEnum>
        where S : CSum<TEnum, TDst>
    {
        TDst Sum(this TColl c) => c.GetEnumerator().Sum();
    }

    public class MonoidInstances
    {
        /// <summary>
        /// Summation over a general enumerator, when the element is a monoid.
        /// Summation is iterated monoid append with monoid empty as a unit.
        /// </summary>
        public instance Sum_Enumerator_Monoid<TEnum, [AssociatedType] TElem, implicit E, implicit M> : CSum<TEnum, TElem>
            where E : CEnumerator<TEnum, TElem>
            where M : Monoid<TElem>
        {
            TElem Sum(this TEnum e)
            {
                var sum = M.Empty;

                while (E.MoveNext(ref e))
                {
                    sum = M.Append(sum, E.Current(ref e));
                }

                return sum;
            }
        }
    }

    /// <summary>
    /// Summing over a general enumerator, when the element is a number.
    /// </summary>
    public instance Sum_Enumerator_Num<TEnum, [AssociatedType] TElem, implicit E, implicit N> : CSum<TEnum, TElem>
        where E : CEnumerator<TEnum, TElem>
        where N : Num<TElem>
    {
        TElem Sum(this TEnum e)
        {
            var sum = N.FromInteger(0);
            var count = 0;

            while (E.MoveNext(ref e))
            {
                count++;
                sum += E.Current(ref e);
            }

            return sum;
        }
    }
}
