using System;
using System.Concepts.Prelude;

namespace BeautifulDifferentiation
{
    using static NumUtils;

    /// <summary>
    ///     A higher-order automatic derivative.
    /// </summary>
    /// <typeparam name="A">
    ///     The underlying numeric representation.
    /// </typeparam>
    public struct HoD<A>
    {
        /// <summary>
        ///     The base value of the automatic derivative.
        /// </summary>
        public A X { get; }

        /// <summary>
        ///    The calculated lazy derivative tower of the value.
        /// </summary>
        public Lazy<HoD<A>> DX { get; }

        /// <summary>
        ///     Constructs a derivative.
        /// </summary>
        /// <param name="x">
        ///     The value of the parameter.
        /// </param>
        /// <param name="dx">
        ///     A factory function generating a tower of derivatives.
        /// </param>
        public HoD(A x, Func<HoD<A>> dx)
        {
            this.X = x;
            this.DX = new Lazy<HoD<A>>(dx);
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
        ///     A <see cref="HoD{A}"/> with the value
        ///     <paramref name="k"/> and first derivative <c>0</c>.
        /// </returns>
        public static HoD<A> Const<implicit NumA>(A k) where NumA : Num<A>
        {
            return new HoD<A>(k, () => Const(Zero<A>()));
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
        ///     A <see cref="HoD{A}"/> with the value
        ///     <paramref name="t"/> and first derivative <c>1</c>.
        /// </returns>
        public static HoD<A> Id<implicit NumA>(A t) where NumA : Num<A>
        {
            return new HoD<A>(t, () => Const(One<A, NumA>()));
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
        public static Func<HoD<A>, HoD<A>> Chain<implicit NumHoDA>(Func<A, A> f, Func<HoD<A>, HoD<A>> df)
            where NumHoDA : Num<HoD<A>>
            => (d) => new HoD<A>(f(d.X), () => Mul(d.DX.Value, df(d)));
    }
}