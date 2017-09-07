using System;
using System.Concepts;

namespace TinyLinq
{
    public static class PrefixedExtensions
    {
        /// <summary>
        /// Performs a Select query using TinyLinq concepts.
        /// </summary>
        /// <typeparam name="TSrc">
        /// Type of the collection being selected over.
        /// </typeparam>
        /// <typeparam name="TDest">
        /// Type of the returned enumerator.
        /// </typeparam>
        /// <typeparam name="TElem">
        /// Type of elements entering the Select.
        /// </typeparam>
        /// <typeparam name="TProj">
        /// Type of elements leaving the Select.
        /// </typeparam>
        /// <typeparam name="M">
        /// The <see cref="CSelect{T, U, S, D}"/> instance in use.
        /// </typeparam>
        /// <param name="This">
        /// The collection being selected over.
        /// </param>
        /// <param name="f">
        /// The selection predicate.
        /// </param>
        /// <returns>
        /// An enumerator performing the Select query.
        /// </returns>
        public static TDest CSelect<TSrc, [AssociatedType]TDest, [AssociatedType]TElem, [AssociatedType]TProj, implicit M>(this TSrc This, Func<TElem, TProj> f) where M : CSelect<TElem, TProj, TSrc, TDest> =>
            M.Select(This, f);

        public static TDest CWhere<TSrc, [AssociatedType]TElem, [AssociatedType]TDest, implicit M>(this TSrc This, Func<TElem, bool> f) where M : CWhere<TSrc, TElem, TDest> =>
            M.Where(This, f);

        public static U[] CToArray<S, [AssociatedType]U, implicit TA>(this S This) where TA : CToArray<S, U>
            => TA.ToArray(This);

        public static int CCount<S, implicit C>(this S This) where C : CCount<S> => C.Count(ref This);

        public static TElem CSum<TEnum, [AssociatedType]TElem, implicit S>(this TEnum e) where S : CSum<TEnum, TElem> => S.Sum(ref e);

        public static TElem CAverage<TEnum, [AssociatedType]TElem, implicit A>(this TEnum e) where A : CAverage<TEnum, TElem> => A.Average(ref e);
    }
}
