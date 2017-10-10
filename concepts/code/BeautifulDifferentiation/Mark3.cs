using System;
using System.Concepts;
using System.Concepts.Prelude;
using static System.Concepts.Prelude.Verbose;

/// <summary>
///     Mark 3 beautiful differentiation.
/// </summary>
namespace BeautifulDifferentiation.Mark3
{
    // TBC for ExpInstances rather than FuncInstances
    using FuncInstances;
    using static NumUtils;

    instance NumDA<A, implicit NumA> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));
        D<A> operator +(D<A> x, D<A> y) => new D<A>(x.X + y.X, x.DX + y.DX);
        D<A> operator *(D<A> x, D<A> y) => new D<A>(x.X * y.X, (x.DX * y.X) + (y.DX * x.X));
        D<A> operator -(D<A> x) => new D<A>(-x.X, -x.DX);
        D<A> operator -(D<A> x, D<A> y) => new D<A>(x.X - y.X, x.DX - y.DX);
        D<A> Signum(D<A> x) => D<A>.Chain(Signum, NumF<A, A>.FromInteger(0))(x);
        D<A> Abs(D<A> x) => D<A>.Chain(Abs, Signum)(x);
    }

    [Overlapping]
    instance FractionalDA<A, implicit FracA> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // TODO: delegate to NumDA somehow
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));
        D<A> operator +(D<A> x, D<A> y) => new D<A>(x.X + y.X, x.DX + y.DX);
        D<A> operator *(D<A> x, D<A> y) => new D<A>(x.X * y.X, (x.DX * y.X) + (y.DX * x.X));
        D<A> operator -(D<A> x) => new D<A>(-x.X, -x.DX);
        D<A> operator -(D<A> x, D<A> y) => new D<A>(x.X - y.X, x.DX - y.DX);
        D<A> Signum(D<A> x) => D<A>.Chain(Signum, NumF<A, A>.FromInteger(0))(x);
        D<A> Abs(D<A> x) => D<A>.Chain(Abs, Signum)(x);
        // End TODO

        D<A> FromRational(Ratio<int> x) => D<A>.Const(FromRational(x));
        D<A> operator /(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   x.X / y.X,
                   ((x.DX * y.X) - (y.DX * x.X)) / (y.X * y.X)
               );
    }

    [Overlapping]
    instance FloatingDA<A, implicit FloatA> : Floating<D<A>>
        where FloatA : Floating<A>
    {
        // TODO: delegate to FractionalDA somehow
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));
        D<A> operator +(D<A> x, D<A> y) => new D<A>(x.X + y.X, x.DX + y.DX);
        D<A> operator *(D<A> x, D<A> y) => new D<A>(x.X * y.X, (x.DX * y.X) + (y.DX * x.X));
        D<A> operator -(D<A> x) => new D<A>(-x.X, -x.DX);
        D<A> operator -(D<A> x, D<A> y) => new D<A>(x.X - y.X, x.DX - y.DX);
        D<A> Signum(D<A> x) => D<A>.Chain(Signum, NumF<A, A>.FromInteger(0))(x);
        D<A> Abs(D<A> x) => D<A>.Chain(Abs, Signum)(x);

        D<A> FromRational(Ratio<int> x) => D<A>.Const(FromRational(x));
        D<A> operator /(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   x.X / y.X,
                   ((x.DX * y.X) - (y.DX * x.X)) / (y.X * y.X)
               );
        // End TODO

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
                   Recip((Two<Func<A, A>>() * Sqrt))
               )(x);

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y) => this.Exp(Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x) => Div(this.Log(x), this.Log(b));

        // d(sin x) = cos x
        D<A> Sin(D<A> x) => D<A>.Chain(Sin, Cos)(x);

        // d(sin x) = -sin x
        D<A> Cos(D<A> x)
            => D<A>.Chain(Cos, -Sin)(x);

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => D<A>.Chain(
                   Tan,
                   One<Func<A, A>>() + Square(Tan)
               )(x);

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => D<A>.Chain(
                   Asin,
                   Recip(
                       FloatF<A, A>.Sqrt(
                           One<Func<A, A>>() - Square
                       )
                   )
               )(x);

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => D<A>.Chain(
                   Acos,
                   Recip(
                       -(
                           FloatF<A, A>.Sqrt(
                               One<Func<A, A>>() - Square
                           )
                       )
                   )
               )(x);

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => D<A>.Chain(
                   Atan,
                   Recip(One<Func<A, A>>() + Square)
               )(x);

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x) => D<A>.Chain(Sinh, Cosh)(x);

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x) => D<A>.Chain(Cosh, Sinh)(x);

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x)
            => D<A>.Chain(Tanh, Recip(Square(Cosh)))(x);

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => D<A>.Chain(
                   Asinh,
                   Recip(
                       FloatF<A, A>.Sqrt(
                           Square + One<Func<A, A>>()
                       )
                   )
               )(x);

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => D<A>.Chain(
                   Acosh,
                   Recip(
                       FloatF<A, A>.Sqrt(
                           Square - One<Func<A, A>>()
                       )
                   )
               )(x);

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => D<A>.Chain(
                   Atanh,
                   Recip(One<Func<A, A>>() - Square)
               )(x);
    }
}
