// This is the version of Monoid for OpPrelude.
// Use Monoid if using Prelude.
using System.Collections.Generic;
using System.Concepts.OpPrelude;

/// <summary>
///     Implementation of monoids, inspired by Haskell.
///     <para>
///         This example mainly exists to show the added expressiveness
///         we get from explicit witnesses.
///     </para>
/// </summary>
namespace System.Concepts.OpMonoid
{
#region Concepts
    /// <summary>
    ///     Concept for semigroups.
    ///     <para>
    ///         A semigroup is a type with an associative binary
    ///         operation.
    ///     </para>
    /// </summary>
    /// <typeparam name="A">
    ///     The type being described as a semigroup.
    /// </typeparam>
    public concept Semigroup<A>
    {
        /// <summary>
        ///     An associative binary operation.
        /// </summary>
        /// <param name="x">
        ///     The first operand to the operation.
        /// </param>
        /// <param name="y">
        ///     The second operand to the operation.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        A Append(A x, A y);
    }

    /// <summary>
    ///     Concept for monoids.
    ///     <para>
    ///         A monoid is a type with an identity and an associative
    ///         binary operation.
    ///     </para>
    /// </summary>
    /// <typeparam name="A">
    ///     The type being described as a monoid.
    /// </typeparam>
    public concept Monoid<A> : Semigroup<A>
    {
        /// <summary>
        ///     The identity of <see cref="Append" />.
        /// <summary>
        A Empty();

        // In Haskell 98, we also have MConcat here.
        // For now, however, we define it separately.
    }

#endregion Concept

#region Instances

    /// <summary>
    ///     Booleans form a monoid under conjunction.
    /// </summary>
    public instance All : Monoid<bool>
    {
        bool Empty() => true;
        bool Append(bool x, bool y) => x && y;
    }

    /// <summary>
    ///     Booleans form a monoid under disjunctions.
    /// </summary>
    public instance Any : Monoid<bool>
    {
        bool Empty() => true;
        bool Append(bool x, bool y) => x || y;
    }

    /// <summary>
    ///     Ordered values form a semigroup under minimum.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the ordered values.
    /// </typeparam>
    public instance Min<A, implicit OrdA> : Semigroup<A> where OrdA : Ord<A>
    {
        // Is this actually associative?
        A Append(A x, A y) => x <= y ? x : y;
    }

    /// <summary>
    ///     Ordered values form a semigroup under maximum.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the ordered values.
    /// </typeparam>
    public instance Max<A, implicit OrdA> : Semigroup<A> where OrdA : Ord<A>
    {
        // Is this actually associative?
        A Append(A x, A y) => x <= y ? y : x;
    }

    /// <summary>
    ///     Numbers form a monoid under addition.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the number being added.
    /// </typeparam>
    public instance Sum<A, implicit NumA> : Monoid<A> where NumA : Num<A>
    {
        A Empty() => FromInteger(0);
        A Append(A x, A y) => x + y;
    }

    /// <summary>
    ///     Numbers form a monoid under multiplication.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the number being multiplied.
    /// </typeparam>
    public instance Product<A, implicit NumA> : Monoid<A> where NumA : Num<A>
    {
        A Empty() => FromInteger(1);
        A Append(A x, A y) => x * y;
    }

#endregion Instances

#region Utilities

    /// <summary>
    ///     Utility functions for monoids and semigroups.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        ///     Folds a non-empty list using a semigroup.
        /// </summary>
        /// <param name="xs">
        ///     The list to fold; must be non-empty.
        /// </param>
        /// <returns>
        ///     The result of folding.
        /// </returns>
        /// <typeparam name="A">
        ///     The semigroup on which this function is being defined.
        /// </typeparam>
        public static A ConcatNonEmpty<A, implicit SA>(IEnumerable<A> xs)
            where SA : Semigroup<A>
        {
            var en = xs.GetEnumerator();
            if (en.MoveNext() == false) throw new ArgumentException(
                "ConcatNonEmpty: list must be non-empty"
            );

            A result = en.Current;
            while (en.MoveNext())
            {
                result = Append(result, en.Current);
            }

            return result;
        }

        /// <summary>
        ///     Applies a function to each item of a non-empty list, and folds
        ///     the results using a semigroup.
        /// </summary>
        /// <param name="xs">
        ///     The list to fold; must be non-empty.
        /// </param>
        /// <param name="f">
        ///     The function to apply on each item.
        /// </param>
        /// <returns>
        ///     The result of folding.
        /// </returns>
        /// <typeparam name="A">
        ///     The semigroup on which this function is being defined.
        /// </typeparam>
        /// <typeparam name="B">
        ///     The domain of the function <paramref name="f" />.
        /// </typeparam>
        public static A ConcatMapNonEmpty<A, B, implicit SA>(IEnumerable<B> xs, Func<B, A> f)
            where SA : Semigroup<A>
        {
            var en = xs.GetEnumerator();
            if (en.MoveNext() == false) throw new ArgumentException(
                "ConcatMapNonEmpty: list must be non-empty"
            );

            A result = f(en.Current);
            while (en.MoveNext())
            {
                result = Append(result, f(en.Current));
            }

            return result;
        }

        /// <summary>
        ///     Folds a list using a monoid.
        /// </summary>
        /// <param name="xs">
        ///     The list to fold.
        /// </param>
        /// <typeparam name="A">
        ///     The monoid on which this function is being defined.
        /// </typeparam>
        /// <returns>
        ///     The result of folding.
        /// </returns>
        public static A Concat<A, implicit MA>(IEnumerable<A> xs)
            where MA : Monoid<A>
        {
            A result = Empty();
            foreach (A x in xs)
            {
                result = Append(result, x);
            }
            return result;
        }

        /// <summary>
        ///     Applies a function to each item of a list, and folds
        ///     the results using a monoid.
        /// </summary>
        /// <param name="xs">
        ///     The list to fold.
        /// </param>
        /// <param name="f">
        ///     The function to apply on each item.
        /// </param>
        /// <returns>
        ///     The result of folding.
        /// </returns>
        /// <typeparam name="A">
        ///     The monoid on which this function is being defined.
        /// </typeparam>
        /// <typeparam name="B">
        ///     The domain of the function <paramref name="f" />.
        /// </typeparam>
        public static A ConcatMap<A, B, implicit MA>(IEnumerable<B> xs, Func<B, A> f)
            where MA : Monoid<A>
        {
            A result = Empty();
            foreach (B x in xs)
            {
                result = Append(result, f(x));
            }
            return result;
        }
    }

#endregion Utilities
}