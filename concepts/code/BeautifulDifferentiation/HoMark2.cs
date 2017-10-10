using System;
using System.Concepts.OpPrelude;
using static System.Concepts.OpPrelude.Verbose;

// Add +
// Mul *
// Sub -
// Div /

/// <summary>
///     Higher-order 2 beautiful differentiation.
/// </summary>
namespace BeautifulDifferentiation.HoMark2
{
    using System.Concepts;
    using FuncInstances;
    using static NumUtils;

    instance NumDA<A, implicit NumA> : Num<HoD<A>>
        where NumA : Num<A>
    {
        HoD<A> FromInteger(int x) => HoD<A>.Const(FromInteger(x));
        HoD<A> operator +(HoD<A> x, HoD<A> y) => new HoD<A>(x.X + y.X, () => x.DX.Value + y.DX.Value);
        HoD<A> operator *(HoD<A> x, HoD<A> y) => new HoD<A>(x.X * y.X, () => (x.DX.Value * y) + (y.DX.Value * x));
        HoD<A> operator -(HoD<A> x, HoD<A> y) => new HoD<A>(x.X - y.X, () => x.DX.Value - y.DX.Value);
        HoD<A> Signum(HoD<A> x) => HoD<A>.Chain(Signum, NumF<HoD<A>, HoD<A>, NumDA<A>>.FromInteger(0))(x);
        HoD<A> Abs(HoD<A> x) => HoD<A>.Chain(Abs, this.Signum)(x);
    }

    [Overlapping]
    instance FractionalDA<A, implicit FracA> : Fractional<HoD<A>>
        where FracA : Fractional<A>
    {
        // TODO: delegate to NumDA somehow
        HoD<A> FromInteger(int x) => HoD<A>.Const(FromInteger(x));
        HoD<A> operator +(HoD<A> x, HoD<A> y) => new HoD<A>(x.X + y.X, () => x.DX.Value + y.DX.Value);
        HoD<A> operator *(HoD<A> x, HoD<A> y) => new HoD<A>(x.X * y.X, () => (x.DX.Value * y) + (y.DX.Value * x));
        HoD<A> operator -(HoD<A> x, HoD<A> y) => new HoD<A>(x.X - y.X, () => x.DX.Value - y.DX.Value);
        HoD<A> Signum(HoD<A> x) => HoD<A>.Chain(Signum, NumF<HoD<A>, HoD<A>, NumDA<A>>.FromInteger(0))(x);
        HoD<A> Abs(HoD<A> x) => HoD<A>.Chain(Abs, this.Signum)(x);
        // End TODO

        HoD<A> FromRational(Ratio<int> x) => HoD<A>.Const(FromRational(x));
        HoD<A> operator /(HoD<A> x, HoD<A> y)
            => new HoD<A>(
                   // Quotient rule
                   x.X / y.X,
                   () => ((x.DX.Value * y) - (y.DX.Value * x)) / Square(y)
               );
    }

    [Overlapping]
    instance FloatingDA<A, implicit FloatA> : Floating<HoD<A>>
        where FloatA : Floating<A>
    {
        // TODO: delegate to FloatingDA somehow
        HoD<A> FromInteger(int x) => HoD<A>.Const(FromInteger(x));
        HoD<A> operator +(HoD<A> x, HoD<A> y) => new HoD<A>(x.X + y.X, () => x.DX.Value + y.DX.Value);
        HoD<A> operator *(HoD<A> x, HoD<A> y) => new HoD<A>(x.X * y.X, () => (x.DX.Value * y) + (y.DX.Value * x));
        HoD<A> operator -(HoD<A> x, HoD<A> y) => new HoD<A>(x.X - y.X, () => x.DX.Value - y.DX.Value);
        HoD<A> Signum(HoD<A> x) => HoD<A>.Chain(Signum, NumF<HoD<A>, HoD<A>, NumDA<A>>.FromInteger(0))(x);
        HoD<A> Abs(HoD<A> x) => HoD<A>.Chain(Abs, this.Signum)(x);
        HoD<A> FromRational(Ratio<int> x) => HoD<A>.Const(FromRational(x));
        HoD<A> operator /(HoD<A> x, HoD<A> y)
            => new HoD<A>(
                   // Quotient rule
                   x.X / y.X,
                   () => ((x.DX.Value * y) - (y.DX.Value * x)) / Square(y)
               );
        // End TODO

        HoD<A> Pi() => HoD<A>.Const(Pi());

        // d(e^x) = e^x
        HoD<A> Exp(HoD<A> x) => HoD<A>.Chain(Exp, this.Exp)(x);

        // d(ln x) = 1/x
        HoD<A> Log(HoD<A> x) => HoD<A>.Chain(Log, Recip)(x);

        // d(sqrt x) = 1/(2 sqrt x)
        HoD<A> Sqrt(HoD<A> x)
            => HoD<A>.Chain(
                   Sqrt,
                   Recip(Two<Func<HoD<A>, HoD<A>>>() * this.Sqrt)
               )(x);

        // d(x^y) rewrites to D(e^(ln x * y))
        HoD<A> Pow(HoD<A> x, HoD<A> y) => this.Exp(Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        HoD<A> LogBase(HoD<A> b, HoD<A> x) => Div(this.Log(x), this.Log(b));

        // d(sin x) = cos x
        HoD<A> Sin(HoD<A> x) => HoD<A>.Chain(Sin, this.Cos)(x);

        // d(sin x) = -sin x
        HoD<A> Cos(HoD<A> x)
            => HoD<A>.Chain(Cos, -this.Sin)(x);

        // d(tan x) = 1 + tan^2 x
        HoD<A> Tan(HoD<A> x)
            => HoD<A>.Chain(
                   Tan,
                   (One<Func<HoD<A>, HoD<A>>>() + Square<Func<HoD<A>, HoD<A>>>(this.Tan))
               )(x);

        // d(asin x) = 1/sqrt(1 - x^2)
        HoD<A> Asin(HoD<A> x)
            => HoD<A>.Chain(
                   Asin,
                   Recip(
                       FloatF<HoD<A>, HoD<A>>.Sqrt(
                           One<Func<HoD<A>, HoD<A>>>() - Square
                       )
                   )
               )(x);

        // d(acos x) = -1/sqrt(1 - x^2)
        HoD<A> Acos(HoD<A> x)
            => HoD<A>.Chain(
                   Acos,
                   Recip(
                       -(
                           FloatF<HoD<A>, HoD<A>>.Sqrt(
                               One<Func<HoD<A>, HoD<A>>>() - Square
                           )
                       )
                   )
               )(x);

        // d(atan x) = 1/(1 + x^2)
        HoD<A> Atan(HoD<A> x)
            => HoD<A>.Chain(
                   Atan,
                   Recip(One<Func<HoD<A>, HoD<A>>>() + Square)
               )(x);

        // d(sinh x) = cosh x
        HoD<A> Sinh(HoD<A> x) => HoD<A>.Chain(Sinh, this.Cosh)(x);

        // d(cosh x) = sinh x
        HoD<A> Cosh(HoD<A> x) => HoD<A>.Chain(Cosh, this.Sinh)(x);

        // d(tanh x) = 1/(cosh^2 x)
        HoD<A> Tanh(HoD<A> x)
            => HoD<A>.Chain(Tanh, Recip(Square(this.Cosh)))(x);

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        HoD<A> Asinh(HoD<A> x)
            => HoD<A>.Chain(
                   Asinh,
                   Recip(
                       FloatF<HoD<A>, HoD<A>>.Sqrt(
                           Square + One<Func<HoD<A>, HoD<A>>>()
                       )
                   )
               )(x);

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        HoD<A> Acosh(HoD<A> x)
            => HoD<A>.Chain(
                   Acosh,
                   Recip(
                       Floating<Func<HoD<A>, HoD<A>>>.Sqrt(
                           Square - One<Func<HoD<A>, HoD<A>>>()
                       )
                   )
               )(x);

        // d(atanh x) = 1 / (1 - x^2)
        HoD<A> Atanh(HoD<A> x)
            => HoD<A>.Chain(
                   Atanh,
                   Recip(One<Func<HoD<A>, HoD<A>>>() - Square)
               )(x);
    }
}
