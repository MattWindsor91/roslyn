using System;
using System.Concepts.OpPrelude;
using static System.Concepts.OpPrelude.Verbose;

/// <summary>
///     Mark 2 beautiful differentiation.
/// </summary>
namespace OpBeautifulDifferentiation.Mark2
{
    using FuncInstances;
    using static NumUtils;

    instance NumDA<A, implicit NumA> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));

        D<A> operator +(D<A> x, D<A> y)
            => new D<A>(Add(x.X, y.X), Add(x.DX, y.DX));


        D<A> operator *(D<A> x, D<A> y)
            // Product rule
            => new D<A>(x.X * y.X, (x.DX * y.X) + (y.DX * x.X));

        D<A> operator -(D<A> x, D<A> y)
            => new D<A>(x.X - y.X, x.DX - y.DX);

        D<A> Signum(D<A> x) => D<A>.Chain(Signum, NumF<A, A>.FromInteger(0))(x);

        D<A> Abs(D<A> x) => D<A>.Chain(Abs, Signum)(x);
    }

    instance FractionalDA<A, implicit FracA> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => NumDA<A>.FromInteger(x);
        D<A> operator +(D<A> x, D<A> y) => Add<D<A>, NumDA<A>>(x, y);
        D<A> operator *(D<A> x, D<A> y) => Mul<D<A>, NumDA<A>>(x, y);
        D<A> operator -(D<A> x, D<A> y) => Sub<D<A>, NumDA<A>>(x, y);
        D<A> Signum(D<A> x)             => NumDA<A>.Signum(x);
        D<A> Abs(D<A> x)                => NumDA<A>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x) => D<A>.Const(FromRational(x));

        D<A> operator /(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   x.X / y.X,
                   ((x.DX * y.X) - (y.DX * x.X)) / (y.X * y.X)
               );
    }

    instance FloatingDA<A, implicit FloatA> : Floating<D<A>>
        where FloatA : Floating<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => FractionalDA<A>.FromInteger(x);
        D<A> operator +(D<A> x, D<A> y) => Add<D<A>, NumDA<A>>(x, y);
        D<A> operator *(D<A> x, D<A> y) => Mul<D<A>, NumDA<A>>(x, y);
        D<A> operator -(D<A> x, D<A> y) => Sub<D<A>, NumDA<A>>(x, y);
        D<A> Signum(D<A> x)             => FractionalDA<A>.Signum(x);
        D<A> Abs(D<A> x)                => FractionalDA<A>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x) => FractionalDA<A>.FromRational(x);
        D<A> operator /(D<A> x, D<A> y) => Div<D<A>, FractionalDA<A>>(x, y);

        // Implementation of Floating
        D<A> Pi() => D<A>.Const(Pi());

        // d(e^x) = e^x
        D<A> Exp(D<A> x) => D<A>.Chain(Exp, Exp)(x);

        // d(ln x) = 1/x
        D<A> Log(D<A> x) => D<A>.Chain(Log, Recip)(x);

        // d(sqrt x) = 1/(2 sqrt x)
        D<A> Sqrt(D<A> x)
            => D<A>.Chain(Sqrt, Recip(Mul(Two<Func<A, A>>(), Sqrt)))(x);

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y) => this.Exp(Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x) => Div(this.Log(x), this.Log(b));

        // d(sin x) = cos x
        D<A> Sin(D<A> x) => D<A>.Chain(Sin, Cos)(x);

        // d(sin x) = -sin x
        D<A> Cos(D<A> x) => D<A>.Chain(Cos, Neg<Func<A, A>>(Sin))(x);

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => D<A>.Chain(
                   Tan, Add(One<Func<A, A>>(), Square<Func<A, A>>(Tan))
               )(x);

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => D<A>.Chain(
                   Asin,
                   Recip(FloatF<A, A>.Sqrt(Sub(One<Func<A, A>>(), Square)))
               )(x);

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => D<A>.Chain(
                   Acos,
                   Recip(
                       Neg(
                           FloatF<A, A>.Sqrt(Sub(One<Func<A, A>>(), Square))
                       )
                   )
               )(x);

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => D<A>.Chain(Atan, Recip(Add(One<Func<A, A>>(), Square)))(x);

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x) => D<A>.Chain(Sinh, Cosh)(x);

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x) => D<A>.Chain(Cosh, Sinh)(x);

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x)
            => D<A>.Chain(Tanh, Recip(Square<Func<A, A>>(Cosh)))(x);

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => D<A>.Chain(
                   Asinh,
                   Recip(FloatF<A, A>.Sqrt(Add(Square, One<Func<A, A>>())))
               )(x);

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => D<A>.Chain(
                   Acosh,
                   Recip(FloatF<A, A>.Sqrt(Sub(Square, One<Func<A, A>>())))
               )(x);

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => D<A>.Chain(Atanh, Recip(Sub(One<Func<A, A>>(), Square)))(x);
    }
}