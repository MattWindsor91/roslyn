using System.Concepts.Prelude;

/// <summary>
///     Mark 1 beautiful differentiation.
/// </summary>
namespace BeautifulDifferentiation.Mark1
{
    using static NumUtils;

    instance NumDA<A, implicit NumA> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));

        D<A> Add(D<A> x, D<A> y) => new D<A>(Add(x.X, y.X), Add(x.DX, y.DX));

        D<A> Mul(D<A> x, D<A> y)
            // Product rule
            => new D<A>(Mul(x.X, y.X), Add(Mul(x.DX, y.X), Mul(y.DX, x.X)));

        D<A> Sub(D<A> x, D<A> y)
            => new D<A>(Sub(x.X, y.X), Sub(x.DX, y.DX));

        D<A> Signum(D<A> x) => new D<A>(Signum(x.X), Zero<A>());

        D<A> Abs(D<A> x) => new D<A>(Abs(x.X), Mul(x.DX, Signum(x.X)));
    }

    instance FractionalDA<A, implicit FracA> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => NumDA<A>.FromInteger(x);
        D<A> Add(D<A> x, D<A> y)        => NumDA<A>.Add(x, y);
        D<A> Mul(D<A> x, D<A> y)        => NumDA<A>.Mul(x, y);
        D<A> Sub(D<A> x, D<A> y)        => NumDA<A>.Sub(x, y);
        D<A> Signum(D<A> x)             => NumDA<A>.Signum(x);
        D<A> Abs(D<A> x)                => NumDA<A>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x) => D<A>.Const(FromRational(x));

        D<A> Div(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   Div(x.X, y.X),
                   Div(
                       Sub(Mul(x.DX, y.X), Mul(y.DX, x.X)),
                       Mul(y.X, y.X)
                   )
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
        D<A> Exp(D<A> x) => new D<A>(Exp(x.X), Mul(x.DX, Exp(x.X)));

        // d(ln x) = 1/x
        D<A> Log(D<A> x) => new D<A>(Log(x.X), Div(x.DX, x.X));

        // d(sqrt x) = 1/(2 sqrt x)
        D<A> Sqrt(D<A> x)
            => new D<A>(Sqrt(x.X), Div(x.DX, Mul(Two<A>(), Sqrt(x.X))));

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y) => this.Exp(this.Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x) => this.Div(this.Log(x), this.Log(b));


        // d(sin x) = cos x
        D<A> Sin(D<A> x) => new D<A>(Sin(x.X), Mul(x.DX, Cos(x.X)));

        // d(sin x) = -sin x
        D<A> Cos(D<A> x) => new D<A>(Cos(x.X), Mul(x.DX, Neg(Sin(x.X))));

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => new D<A>(
                   Tan(x.X), Mul(x.DX, Add(One<A>(), Square(Tan(x.X))))
               );

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => new D<A>(
                   Asin(x.X), Div(x.DX, Sqrt(Sub(One<A>(), Square(x.X))))
               );

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => new D<A>(
                   Acos(x.X), Div(x.DX, Neg(Sqrt(Sub(One<A>(), Square(x.X)))))
               );

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => new D<A>(Atan(x.X), Div(x.DX, Add(One<A>(), Square(x.X))));

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x) => new D<A>(Sinh(x.X), Mul(x.DX, Cosh(x.X)));

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x) => new D<A>(Cosh(x.X), Mul(x.DX, Sinh(x.X)));

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x) => new D<A>(Tanh(x.X), Div(x.DX, Square(Cosh(x.X))));

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => new D<A>(
                   Asinh(x.X),
                   Div(x.DX, Sqrt(Add(Square(x.X), One<A>())))
               );

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => new D<A>(
                   Acosh(x.X),
                   Div(x.DX, Sqrt(Sub(Square(x.X), One<A>())))
               );

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => new D<A>(
                   Atanh(x.X),
                   Div(x.DX, Sub(One<A>(), Square(x.X)))
               );
    }
}