using System.Concepts;
using System.Concepts.Enumerable;
using System.Concepts.Monoid;
using System.Concepts.Prelude;

namespace TinyLinq
{
    /// <summary>
    /// Concept for summing over an enumerable.
    /// </summary>
    public concept CSum<TColl, [AssociatedType] TElem>
    {
        /// <summary>
        /// Sums over all of the elements of an enumerable.
        /// </summary>
        /// <param name="source">
        /// The enumerator to sum.
        /// </param>
        /// <returns>
        /// The sum of all elements in the enumerable
        /// </returns>
        TElem Sum(this TColl source);        
    }

    public class MonoidInstances
    {
        /// <summary>
        /// Summation over a general enumerator, when the element is a monoid.
        /// Summation is iterated monoid append with monoid empty as a unit.
        /// </summary>
        public instance Sum_Enumerable_Monoid<TSourceColl, [AssociatedType]TSourceEnum, [AssociatedType]TSource, implicit Eb, implicit Et, implicit M> : CSum<TSourceColl, TSource>
            where Eb : CEnumerable<TSourceColl, TSourceEnum>
            where Et : CEnumerator<TSourceEnum, TSource>
            where M : Monoid<TSource>
        {
            TSource Sum(this TSourceColl source)
            {
                var e = source.RefGetEnumerator();

                var sum = M.Empty;
                while (e.MoveNext())
                {
                    sum = sum.Append(e.Current());
                }

                return sum;
            }
        }
    }

    /// <summary>
    /// Summing over a general enumerable, when the element is a number.
    /// </summary>
    [Overlappable]
    public instance Sum_Enumerable_Num<TSourceColl, [AssociatedType] TSourceEnum, [AssociatedType]TSource, implicit Eb, implicit Et, implicit N> : CSum<TSourceColl, TSource>
        where Eb : CEnumerable<TSourceColl, TSourceEnum>
        where Et : CEnumerator<TSourceEnum, TSource>
        where N : Num<TSource>
    {
        TSource Sum(this TSourceColl source)
        {
            var e = source.RefGetEnumerator();

            var sum = N.FromInteger(0);
            while (e.MoveNext())
            {
                sum += e.Current();
            }

            return sum;
        }
    }
}
