using System;
using System.Concepts.Prelude;

/// <summary>
///     Mark 2 beautiful differentiation.
/// </summary>
namespace BeautifulDifferentiation.Mark2
{
    using FuncInstances;
    using static NumUtils;

    instance NumDA<A, implicit NumA> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));

        D<A> Add(D<A> x, D<A> y)
            => new D<A>(Add(x.X, y.X), Add(x.DX, y.DX));

        D<A> Mul(D<A> x, D<A> y)
            // Product rule
            => new D<A>(Mul(x.X, y.X), Add(Mul(x.DX, y.X), Mul(y.DX, x.X)));

        D<A> Sub(D<A> x, D<A> y)
            => new D<A>(Sub(x.X, y.X), Sub(x.DX, y.DX));

        D<A> Signum(D<A> x) =>
            D<A>.Chain(Signum, NumF<A, A>.FromInteger(0))(x);

        D<A> Abs(D<A> x) => D<A>.Chain(Abs, Signum)(x);
    }

    instance FractionalDA<A, implicit FracA> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)  => NumDA<A>.FromInteger(x);
        D<A> Add(D<A> x, D<A> y) => NumDA<A>.Add(x, y);
        D<A> Mul(D<A> x, D<A> y) => NumDA<A>.Mul(x, y);
        D<A> Sub(D<A> x, D<A> y) => NumDA<A>.Sub(x, y);
        D<A> Signum(D<A> x)      => NumDA<A>.Signum(x);
        D<A> Abs(D<A> x)         => NumDA<A>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x) => D<A>.Const(FromRational(x));

        D<A> Div(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   Div(x.X, y.X),
                   Div(Sub(Mul(x.DX, y.X), Mul(y.DX, x.X)), Mul(y.X, y.X))
               );
    }

    instance FloatingDA<A, implicit FloatA> : Floating<D<A>>
        where FloatA : Floating<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => FractionalDA<A>.FromInteger(x);
        D<A> Add(D<A> x, D<A> y)        => FractionalDA<A>.Add(x, y);
        D<A> Mul(D<A> x, D<A> y)        => FractionalDA<A>.Mul(x, y);
        D<A> Sub(D<A> x, D<A> y)        => FractionalDA<A>.Sub(x, y);
        D<A> Signum(D<A> x)             => FractionalDA<A>.Signum(x);
        D<A> Abs(D<A> x)                => FractionalDA<A>.Abs(x);
        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x) => FractionalDA<A>.FromRational(x);
        D<A> Div(D<A> x, D<A> y)        => FractionalDA<A>.Div(x, y);

        // Implementation of Floating
        D<A> Pi() => D<A>.Const(Pi());

        // d(e^x) = e^x
        D<A> Exp(D<A> x) => D<A>.Chain(Exp, Exp)(x);

        // d(ln x) = 1/x
        D<A> Log(D<A> x) => D<A>.Chain(Log, Recip)(x);

        // d(sqrt x) = 1/(2 sqrt x)
        D<A> Sqrt(D<A> x)
            => D<A>.Chain(
                   Sqrt,
                   FloatF<A, A>.Recip(FloatF<A, A>.Mul(Two<Func<A, A>>(), Sqrt))
               )(x);

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y) => this.Exp(this.Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x) => this.Div(this.Log(x), this.Log(b));

        // d(sin x) = cos x
        D<A> Sin(D<A> x) => D<A>.Chain(Sin, Cos)(x);

        // d(sin x) = -sin x
        D<A> Cos(D<A> x)
            => D<A>.Chain(Cos, FloatF<A, A>.Neg(Sin))(x);

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => D<A>.Chain(
                   Tan,
                   FloatF<A, A>.Add(
                       One<Func<A, A>>(), Square<Func<A, A>>(Tan)
                   )
               )(x);

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => D<A>.Chain(
                   Asin,
                   FloatF<A, A>.Recip(
                       FloatF<A, A>.Sqrt(
                           NumF<A, A>.Sub(One<Func<A, A>>(), Square)
                       )
                   )
               )(x);

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => D<A>.Chain(
                   Acos,
                   FloatF<A, A>.Recip(
                       FloatF<A, A>.Neg(
                           FloatF<A, A>.Sqrt(
                               NumF<A, A>.Sub(One<Func<A, A>>(), Square)
                           )
                       )
                   )
               )(x);

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => D<A>.Chain(
                   Atan,
                   FloatF<A, A>.Recip(NumF<A, A>.Add(One<Func<A, A>>(), Square))
               )(x);

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x) => D<A>.Chain(Sinh, Cosh)(x);

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x) => D<A>.Chain(Cosh, Sinh)(x);

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x)
            => D<A>.Chain(Tanh, FloatF<A, A>.Recip(Square<Func<A, A>>(Cosh)))(x);

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => D<A>.Chain(
                   Asinh,
                   FloatF<A, A>.Recip(
                       FloatF<A, A>.Sqrt(
                           NumF<A, A>.Add(Square, One<Func<A, A>>())
                       )
                   )
               )(x);

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => D<A>.Chain(
                   Acosh,
                   FloatF<A, A>.Recip(
                       FloatF<A, A>.Sqrt(
                           NumF<A, A>.Sub(Square, One<Func<A, A>>())
                       )
                   )
               )(x);

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => D<A>.Chain(
                   Atanh,
                   FloatF<A, A>.Recip(NumF<A, A>.Sub(One<Func<A, A>>(), Square))
               )(x);
    }
}