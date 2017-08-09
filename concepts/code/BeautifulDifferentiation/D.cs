using System;
using System.Concepts.Prelude;

namespace BeautifulDifferentiation
{
    using static NumUtils;

    /// <summary>
    ///     A first-order, scalar automatic derivative.
    /// </summary>
    /// <typeparam name="A">
    ///     The underlying numeric representation.
    /// </typeparam>
    public struct D<A>
    {
        /// <summary>
        ///     The base value of the automatic derivative.
        /// </summary>
        public A X { get; }

        /// <summary>
        ///    The calculated first derivative of the value.
        /// </summary>
        public A DX { get; }

        /// <summary>
        ///     Constructs a derivative.
        /// </summary>
        /// <param name="x">
        ///     The base value of the automatic derivative.
        /// </param>
        /// <param name="dx">
        ///    The calculated first derivative of the value.
        /// </param>
        public D(A x, A dx)
        {
            X = x;
            DX = dx;
        }

        /// <summary>
        ///     Constructs a derivative for a constant.
        /// <summary>
        /// <param name="k">
        ///     The constant; must be <c>Num</c>.
        /// </param>
        /// <typeparam name="A">
        ///     The type of <paramref name="k"/> and thus the underlying
        ///     representation of the result.
        /// </typeparam>
        /// <returns>
        ///     A <see cref="D{A}"/> with the value
        ///     <paramref name="k"/> and first derivative <c>0</c>.
        /// </returns>
        public static D<A> Const<implicit NumA>(A k) where NumA : Num<A>
        {
            return new D<A>(k, Zero<A>());
        }

        /// <summary>
        ///     Constructs a derivative for a term.
        /// <summary>
        /// <param name="t">
        ///     The term; must be <c>Num</c>.
        /// </param>
        /// <typeparam name="A">
        ///     The type of <paramref name="t"/> and thus the underlying
        ///     representation of the result.
        /// </typeparam>
        /// <returns>
        ///     A <see cref="D{A}"/> with the value
        ///     <paramref name="t"/> and first derivative <c>1</c>.
        /// </returns>
        public static D<A> Id<implicit NumA>(A t) where NumA : Num<A>
        {
            return new D<A>(t, One<A>());
        }

        /// <summary>
        ///     Scalar chain rule.
        /// </summary>
        /// <param name="f">
        ///     A function.
        /// </param>
        /// <param name="df">
        ///     The derivative of <paramref name="f" />.
        /// </param>
        /// <typeparam name="A">
        ///     The underlying number representation.
        /// </typeparam>
        /// <returns>
        ///     A function over automatic derivatives applying the
        ///     function and its derivative.
        /// </returns>
        public static Func<D<A>, D<A>> Chain<implicit NumA>(Func<A, A> f, Func<A, A> df)
            where NumA : Num<A>
            => (d) => new D<A>(f(d.X), Mul(d.DX, df(d.X)));
    }
}