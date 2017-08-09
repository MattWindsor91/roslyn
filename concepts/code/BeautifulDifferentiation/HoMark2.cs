using System;
using System.Concepts.Prelude;

/// <summary>
///     Mark 2 beautiful differentiation.
/// </summary>
namespace BeautifulDifferentiation.HoMark2
{
    using FuncInstances;
    using static NumUtils;

    instance NumDA<A, implicit NumA> : Num<HoD<A>>
        where NumA : Num<A>
    {
        HoD<A> FromInteger(int x) => HoD<A>.Const(FromInteger(x));

        HoD<A> Add(HoD<A> x, HoD<A> y)
            => new HoD<A>(Add(x.X, y.X), () => NumDA<A>.Add(x.DX.Value, y.DX.Value));

        HoD<A> Mul(HoD<A> x, HoD<A> y)
            // Product rule
            => new HoD<A>(Mul(x.X, y.X), () => NumDA<A>.Add(NumDA<A>.Mul(x.DX.Value, y), NumDA<A>.Mul(y.DX.Value, x)));

        HoD<A> Sub(HoD<A> x, HoD<A> y)
            => new HoD<A>(Sub(x.X, y.X), () => NumDA<A>.Sub(x.DX.Value, y.DX.Value));

        HoD<A> Signum(HoD<A> x) =>
            HoD<A>.Chain(Signum, NumF<HoD<A>, HoD<A>, NumDA<A>>.FromInteger(0))(x);

        HoD<A> Abs(HoD<A> x) => HoD<A>.Chain(Abs, this.Signum)(x);
    }

    instance FractionalDA<A, implicit FracA> : Fractional<HoD<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        HoD<A> FromInteger(int x)      => NumDA<A>.FromInteger(x);
        HoD<A> Add(HoD<A> x, HoD<A> y) => NumDA<A>.Add(x, y);
        HoD<A> Mul(HoD<A> x, HoD<A> y) => NumDA<A>.Mul(x, y);
        HoD<A> Sub(HoD<A> x, HoD<A> y) => NumDA<A>.Sub(x, y);
        HoD<A> Signum(HoD<A> x)        => NumDA<A>.Signum(x);
        HoD<A> Abs(HoD<A> x)           => NumDA<A>.Abs(x);

        // Implementation of Fractional
        HoD<A> FromRational(Ratio<int> x) => HoD<A>.Const(FromRational(x));

        HoD<A> Div(HoD<A> x, HoD<A> y)
            => new HoD<A>(
                   // Quotient rule
                   Div(x.X, y.X),
                   () => FractionalDA<A>.Div(FractionalDA<A>.Sub(FractionalDA<A>.Mul(x.DX.Value, y), FractionalDA<A>.Mul(y.DX.Value, x)), FractionalDA<A>.Mul(y, y))
               );
    }

    instance FloatingDA<A, implicit FloatA> : Floating<HoD<A>>
        where FloatA : Floating<A>
    {
        // Implementation of Num
        HoD<A> FromInteger(int x)         => FractionalDA<A>.FromInteger(x);
        HoD<A> Add(HoD<A> x, HoD<A> y)    => FractionalDA<A>.Add(x, y);
        HoD<A> Mul(HoD<A> x, HoD<A> y)    => FractionalDA<A>.Mul(x, y);
        HoD<A> Sub(HoD<A> x, HoD<A> y)    => FractionalDA<A>.Sub(x, y);
        HoD<A> Signum(HoD<A> x)           => FractionalDA<A>.Signum(x);
        HoD<A> Abs(HoD<A> x)              => FractionalDA<A>.Abs(x);
        // Implementation of Fractional
        HoD<A> FromRational(Ratio<int> x) => FractionalDA<A>.FromRational(x);
        HoD<A> Div(HoD<A> x, HoD<A> y)    => FractionalDA<A>.Div(x, y);

        // Implementation of Floating
        HoD<A> Pi() => HoD<A>.Const(Pi());

        // d(e^x) = e^x
        HoD<A> Exp(HoD<A> x) => HoD<A>.Chain(Exp, this.Exp)(x);

        // d(ln x) = 1/x
        HoD<A> Log(HoD<A> x) => HoD<A>.Chain(Log, this.Recip)(x);

        // d(sqrt x) = 1/(2 sqrt x)
        HoD<A> Sqrt(HoD<A> x)
            => HoD<A>.Chain(
                   Sqrt,
                   FloatF<HoD<A>, HoD<A>>.Recip(FloatF<HoD<A>, HoD<A>>.Mul(Two<Func<HoD<A>, HoD<A>>>(), this.Sqrt))
               )(x);

        // d(x^y) rewrites to D(e^(ln x * y))
        HoD<A> Pow(HoD<A> x, HoD<A> y) => this.Exp(this.Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        HoD<A> LogBase(HoD<A> b, HoD<A> x) => this.Div(this.Log(x), this.Log(b));

        // d(sin x) = cos x
        HoD<A> Sin(HoD<A> x) => HoD<A>.Chain(Sin, this.Cos)(x);

        // d(sin x) = -sin x
        HoD<A> Cos(HoD<A> x)
            => HoD<A>.Chain(Cos, FloatF<HoD<A>, HoD<A>>.Neg(this.Sin))(x);

        // d(tan x) = 1 + tan^2 x
        HoD<A> Tan(HoD<A> x)
            => HoD<A>.Chain(
                   Tan,
                   FloatF<HoD<A>, HoD<A>>.Add(
                       One<Func<HoD<A>, HoD<A>>>(), Square<Func<HoD<A>, HoD<A>>>(this.Tan)
                   )
               )(x);

        // d(asin x) = 1/sqrt(1 - x^2)
        HoD<A> Asin(HoD<A> x)
            => HoD<A>.Chain(
                   Asin,
                   FloatF<HoD<A>, HoD<A>>.Recip(
                       FloatF<HoD<A>, HoD<A>>.Sqrt(
                           NumF<HoD<A>, HoD<A>>.Sub(One<Func<HoD<A>, HoD<A>>>(), Square)
                       )
                   )
               )(x);

        // d(acos x) = -1/sqrt(1 - x^2)
        HoD<A> Acos(HoD<A> x)
            => HoD<A>.Chain(
                   Acos,
                   FloatF<HoD<A>, HoD<A>>.Recip(
                        FloatF<HoD<A>, HoD<A>>.Neg(
                           FloatF<HoD<A>, HoD<A>>.Sqrt(
                               NumF<HoD<A>, HoD<A>>.Sub(One<Func<HoD<A>, HoD<A>>>(), Square)
                           )
                       )
                   )
               )(x);

        // d(atan x) = 1/(1 + x^2)
        HoD<A> Atan(HoD<A> x)
            => HoD<A>.Chain(
                   Atan,
                   FloatF<HoD<A>, HoD<A>>.Recip(NumF<HoD<A>, HoD<A>>.Add(One<Func<HoD<A>, HoD<A>>>(), Square))
               )(x);

        // d(sinh x) = cosh x
        HoD<A> Sinh(HoD<A> x) => HoD<A>.Chain(Sinh, this.Cosh)(x);

        // d(cosh x) = sinh x
        HoD<A> Cosh(HoD<A> x) => HoD<A>.Chain(Cosh, this.Sinh)(x);

        // d(tanh x) = 1/(cosh^2 x)
        HoD<A> Tanh(HoD<A> x)
            => HoD<A>.Chain(Tanh, FloatF<HoD<A>, HoD<A>>.Recip(Square<Func<HoD<A>, HoD<A>>>(this.Cosh)))(x);

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        HoD<A> Asinh(HoD<A> x)
            => HoD<A>.Chain(
                   Asinh,
                   FloatF<HoD<A>, HoD<A>>.Recip(
                       FloatF<HoD<A>, HoD<A>>.Sqrt(
                           NumF<HoD<A>, HoD<A>>.Add(Square, One<Func<HoD<A>, HoD<A>>>())
                       )
                   )
               )(x);

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        HoD<A> Acosh(HoD<A> x)
            => HoD<A>.Chain(
                   Acosh,
                   Fractional<Func<HoD<A>, HoD<A>>>.Recip(
                       Floating<Func<HoD<A>, HoD<A>>>.Sqrt(
                           Num<Func<HoD<A>, HoD<A>>>.Sub(Square, One<Func<HoD<A>, HoD<A>>>())
                       )
                   )
               )(x);

        // d(atanh x) = 1 / (1 - x^2)
        HoD<A> Atanh(HoD<A> x)
            => HoD<A>.Chain(
                   Atanh,
                   Floating<Func<HoD<A>, HoD<A>>>.Recip(NumF<HoD<A>, HoD<A>>.Sub(One<Func<HoD<A>, HoD<A>>>(), Square))
               )(x);
    }
}