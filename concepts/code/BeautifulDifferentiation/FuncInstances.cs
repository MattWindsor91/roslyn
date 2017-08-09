using System;
using System.Concepts.Prelude;

/// <summary>
///     Numeric tower instances for functions.
/// </summary>
namespace BeautifulDifferentiation.FuncInstances
{
    /// <summary>
    ///     Numeric instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Num</c>.
    /// </typeparam>
    instance NumF<A, B, implicit NumB> : Num<Func<A, B>>
        where NumB : Num<B>
    {
        Func<A, B> Add(Func<A, B> f, Func<A, B> g)
            => (x) => Add(f(x), g(x));
        Func<A, B> Sub(Func<A, B> f, Func<A, B> g)
            => (x) => Sub(f(x), g(x));
        Func<A, B> Neg(Func<A, B> f)
            => (x) => Neg(f(x));
        Func<A, B> Mul(Func<A, B> f, Func<A, B> g)
            => (x) => Mul(f(x), g(x));
        Func<A, B> Abs(Func<A, B> f)
            => (x) => Abs(f(x));
        Func<A, B> Signum(Func<A, B> f)
            => (x) => Signum(f(x));
        Func<A, B> FromInteger(int k)
            => (x) => FromInteger(k);
    }

    /// <summary>
    ///     Fractional instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Fractional</c>.
    /// </typeparam>
    instance FracF<A, B, implicit FracB> : Fractional<Func<A, B>>
        where FracB : Fractional<B>
    {
        Func<A, B> Add(Func<A, B> f, Func<A, B> g) => NumF<A, B>.Add(f, g);
        Func<A, B> Neg(Func<A, B> f) => NumF<A, B>.Neg(f);
        Func<A, B> Sub(Func<A, B> f, Func<A, B> g) => NumF<A, B>.Sub(f, g);
        Func<A, B> Mul(Func<A, B> f, Func<A, B> g) => NumF<A, B>.Mul(f, g);
        Func<A, B> Abs(Func<A, B> f) => NumF<A, B>.Abs(f);
        Func<A, B> Signum(Func<A, B> f) => NumF<A, B>.Signum(f);
        Func<A, B> FromInteger(int k) => NumF<A, B>.FromInteger(k);

        Func<A, B> FromRational(Ratio<int> k)
            => (x) => FromRational(k);
        Func<A, B> Div(Func<A, B> f, Func<A, B> g)
            => (x) => Div(f(x), g(x));
        Func<A, B> Recip(Func<A, B> f)
            => (x) => Recip(f(x));
    }

    /// <summary>
    ///     Floating instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Floating</c>.
    /// </typeparam>
    instance FloatF<A, B, implicit FloatB> : Floating<Func<A, B>>
        where FloatB : Floating<B>
    {
        Func<A, B> Add(Func<A, B> f, Func<A, B> g) => FracF<A, B, FloatB>.Add(f, g);
        Func<A, B> Sub(Func<A, B> f, Func<A, B> g) => FracF<A, B, FloatB>.Sub(f, g);
        Func<A, B> Neg(Func<A, B> f) => FracF<A, B, FloatB>.Neg(f);
        Func<A, B> Mul(Func<A, B> f, Func<A, B> g) => FracF<A, B, FloatB>.Mul(f, g);
        Func<A, B> Abs(Func<A, B> f) => FracF<A, B>.Abs(f);
        Func<A, B> Signum(Func<A, B> f) => FracF<A, B>.Signum(f);
        Func<A, B> FromInteger(int k) => FracF<A, B, FloatB>.FromInteger(k);
        Func<A, B> FromRational(Ratio<int> k) => FracF<A, B, FloatB>.FromRational(k);
        Func<A, B> Div(Func<A, B> f, Func<A, B> g) => FracF<A, B, FloatB>.Div(f, g);
        Func<A, B> Recip(Func<A, B> f) => FracF<A, B, FloatB>.Recip(f);

        Func<A, B> Pi() => (x) => Pi();
        Func<A, B> Sqrt(Func<A, B> f) => (x) => Sqrt(f(x));
        Func<A, B> Exp(Func<A, B> f) => (x) => Exp(f(x));
        Func<A, B> Log(Func<A, B> f) => (x) => Log(f(x));
        Func<A, B> Pow(Func<A, B> f, Func<A, B> g)
            => (x) => Pow(f(x), g(x));
        Func<A, B> LogBase(Func<A, B> f, Func<A, B> g)
            => (x) => LogBase(f(x), g(x));

        Func<A, B> Sin(Func<A, B> f) => (x) => Sin(f(x));
        Func<A, B> Cos(Func<A, B> f) => (x) => Cos(f(x));
        Func<A, B> Tan(Func<A, B> f) => (x) => Tan(f(x));
        Func<A, B> Asin(Func<A, B> f) => (x) => Asin(f(x));
        Func<A, B> Acos(Func<A, B> f) => (x) => Acos(f(x));
        Func<A, B> Atan(Func<A, B> f) => (x) => Atan(f(x));
        Func<A, B> Sinh(Func<A, B> f) => (x) => Sinh(f(x));
        Func<A, B> Cosh(Func<A, B> f) => (x) => Cosh(f(x));
        Func<A, B> Tanh(Func<A, B> f) => (x) => Tanh(f(x));
        Func<A, B> Asinh(Func<A, B> f) => (x) => Asinh(f(x));
        Func<A, B> Acosh(Func<A, B> f) => (x) => Acosh(f(x));
        Func<A, B> Atanh(Func<A, B> f) => (x) => Atanh(f(x));
    }

}