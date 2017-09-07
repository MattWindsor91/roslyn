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
        TElem Sum(ref TEnum e);        
    }

    public instance MonoidInstances
    {
        /// <summary>
        /// Summation over a general enumerator, when the element is a monoid.
        /// Summation is iterated monoid append with monoid empty as a unit.
        /// </summary>
        public instance Sum_Enumerator_Monoid<TEnum, [AssociatedType] TElem, implicit E, implicit M> : CSum<TEnum, TElem>
            where E : CEnumerator<TEnum, TElem>
            where M : Monoid<TElem>
        {
            TElem Sum(ref TEnum e)
            {
                var sum = M.Empty;

                E.Reset(ref e);
                while (E.MoveNext(ref e))
                {
                    sum = M.Append(sum, E.Current(ref e));
                }

                return sum;
            }
        }

        /// <summary>
        /// Summation over a general enumerable, when the element is a monoid.
        /// Summation is iterated monoid append with monoid empty as a unit.
        /// </summary>
        public instance Sum_Enumerator_Monoid<TColl, [AssociatedType] TEnum, [AssociatedType] TElem, implicit E, implicit M> : CSum<TColl, TElem>
            where E : CEnumerable<TColl, TEnum, TElem>
            where M : Monoid<TElem>
        {
            TElem Sum(ref TColl c)
            {
                var e = E.GetEnumerator(c);

                var sum = M.Empty;

                E.Reset(ref e);
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
        TElem Sum(ref TEnum e)
        {
            var sum = N.FromInteger(0);
            var count = 0;

            E.Reset(ref e);
            while (E.MoveNext(ref e))
            {
                count++;
                sum = N.Add(sum, E.Current(ref e));
            }

            return sum;
        }
    }

    /// <summary>
    /// Summation over a general enumerable, when the element is a number
    /// </summary>
    public instance Sum_Enumerable_Num<TColl, [AssociatedType] TEnum, [AssociatedType] TElem, implicit E, implicit N> : CSum<TColl, TElem>
        where E : CEnumerable<TColl, TEnum, TElem>
        where N : Num<TElem>
    {
        TElem Sum(ref TColl c)
        {
            var e = E.GetEnumerator(c);

            var sum = N.FromInteger(0);
            var count = 0;

            E.Reset(ref e);
            while (E.MoveNext(ref e))
            {
                count++;
                sum = N.Add(sum, E.Current(ref e));
            }

            return sum;
        }
    }
}
