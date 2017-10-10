using System;
using System.Concepts;
using System.Concepts.OpPrelude;

using ExpressionUtils;
using static ExpressionUtils.Utils;
/// <summary>
///     Numeric tower instances for functions.
/// </summary>
namespace BeautifulDifferentiation.ExpInstances
{
    [Overlapping]
    public instance ExpDouble : Floating<Exp<double>>
    {
        //
        // Num (via Floating)
        //
        Exp<double> operator +(Exp<double> x, Exp<double> y) =>
               Prim((d1, d2) => d1 + d2, x, y);
        Exp<double> operator -(Exp<double> x) =>
               Prim(d1 => -d1, x);
        Exp<double> operator -(Exp<double> x, Exp<double> y) =>
               Prim((d1, d2) => d1 - d2, x, y);
        Exp<double> operator *(Exp<double> x, Exp<double> y) =>
               Prim((d1, d2) => d1 * d2, x, y);
        Exp<double> Abs(Exp<double> x) =>
               Prim(d => Math.Abs(d), x);
        Exp<double> Signum(Exp<double> x) =>
               Prim<double, double>(d => Math.Sign(d), x);
        Exp<double> FromInteger(int x)
            => new Constant<double>(x);

        //
        // Fractional (via Floating)
        //
        Exp<double> operator /(Exp<double> x, Exp<double> y) =>
            Prim((d1, d2) => d1 / d2, x, y);
        Exp<double> FromRational(Ratio<int> x) =>
            new Constant<double>(x.num / x.den);
        //
        // Floating
        //
        Exp<double> Pi() => new Constant<double>(Math.PI);
        Exp<double> Exp(Exp<double> x) =>
            Prim(d => Math.Exp(d), x);
        Exp<double> Sqrt(Exp<double> x) =>
            Prim(d => Math.Sqrt(d), x);
        Exp<double> Log(Exp<double> x) =>
            Prim(d => Math.Log(d), x);
        Exp<double> Pow(Exp<double> x, Exp<double> y) =>
            Prim((d1, d2) => Math.Pow(d1, d2), x, y);
        // Haskell and C# put the base in different places.
        // Maybe we should adopt the C# version?
        Exp<double> LogBase(Exp<double> b, Exp<double> x) =>
            Prim((d1, d2) => Math.Log(d1, d2), x, b);
        Exp<double> Sin(Exp<double> x) =>
            Prim(d => Math.Sin(d), x);
        Exp<double> Cos(Exp<double> x) =>
            Prim(d => Math.Cos(d), x);
        Exp<double> Tan(Exp<double> x) =>
            Prim(d => Math.Tan(d), x);
        Exp<double> Asin(Exp<double> x) =>
            Prim(d => Math.Asin(d), x);
        Exp<double> Acos(Exp<double> x) =>
            Prim(d => Math.Acos(d), x);
        Exp<double> Atan(Exp<double> x) =>
            Prim(d => Math.Atan(d), x);
        Exp<double> Sinh(Exp<double> x) =>
            Prim(d => Math.Sinh(d), x);
        Exp<double> Cosh(Exp<double> x) =>
            Prim(d => Math.Cosh(d), x);
        Exp<double> Tanh(Exp<double> x) =>
            Prim(d => Math.Tanh(d), x);
        // Math doesn't have these, so define them directly in terms of
        // logarithms.
        Exp<double> Asinh(Exp<double> x) =>
            Prim(d => Math.Log(d + Math.Sqrt((d * d) + 1.0)), x);
        Exp<double> Acosh(Exp<double> x) =>
            Prim(d => Math.Log(d + Math.Sqrt((d * d) - 1.0)), x);
        Exp<double> Atanh(Exp<double> x) =>
            Prim(d => 0.5 * Math.Log((1.0 + d) / (1.0 - d)), x);
    }
    
    /// <summary>
    ///     Numeric instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Num</c>.
    /// </typeparam>
    instance NumF<A, B, implicit NumB> : Num<Exp<Func<A, B>>>
            where NumB : Num<Exp<B>>
    {
        Exp<Func<A, B>> operator +(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) + g.Apply(x));
        Exp<Func<A, B>> operator -(Exp<Func<A, B>> f)
            => Lam<A, B>(x => - f.Apply(x));
        Exp<Func<A, B>> operator -(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) - g.Apply(x));
        Exp<Func<A, B>> operator *(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) * g.Apply(x));
        Exp<Func<A, B>> Abs(Exp<Func<A, B>> f)
            => Lam<A, B>(x => Abs(f.Apply(x)));
        Exp<Func<A, B>> Signum(Exp<Func<A, B>> f)
            => Lam<A, B>(x => Signum(f.Apply(x)));
        Exp<Func<A, B>> FromInteger(int k)
            => Lam<A, B>(x => FromInteger(k));
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
    instance FracF<A, B, implicit FracB> : Fractional<Exp<Func<A, B>>>
        where FracB : Fractional<Exp<B>>
    {
        // TODO: delegate to NumF somehow
        Exp<Func<A, B>> operator +(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) + g.Apply(x));
        Exp<Func<A, B>> operator -(Exp<Func<A, B>> f)
            => Lam<A, B>(x => -f.Apply(x));
        Exp<Func<A, B>> operator -(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) - g.Apply(x));
        Exp<Func<A, B>> operator *(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) * g.Apply(x));
        Exp<Func<A, B>> Abs(Exp<Func<A, B>> f)
            => Lam<A, B>(x => Abs(f.Apply(x)));
        Exp<Func<A, B>> Signum(Exp<Func<A, B>> f)
            => Lam<A, B>(x => Signum(f.Apply(x)));
        Exp<Func<A, B>> FromInteger(int k)
            => Lam<A, B>(x => FromInteger(k));
        // End TODO

        Exp<Func<A, B>> FromRational(Ratio<int> k)
             => Lam<A, B>(x => FromRational(k));
        Exp<Func<A, B>> operator /(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
             => Lam<A, B>(x => f.Apply(x) / g.Apply(x));
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
    instance FloatF<A, B, implicit FloatB> : Floating<Exp<Func<A, B>>>
        where FloatB : Floating<Exp<B>>
    {
        // TODO: delegate to FracF somehow
        Exp<Func<A, B>> operator +(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) + g.Apply(x));
        Exp<Func<A, B>> operator -(Exp<Func<A, B>> f)
            => Lam<A, B>(x => -f.Apply(x));
        Exp<Func<A, B>> operator -(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) - g.Apply(x));
        Exp<Func<A, B>> operator *(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => f.Apply(x) * g.Apply(x));
        Exp<Func<A, B>> Abs(Exp<Func<A, B>> f)
            => Lam<A, B>(x => Abs(f.Apply(x)));
        Exp<Func<A, B>> Signum(Exp<Func<A, B>> f)
            => Lam<A, B>(x => Signum(f.Apply(x)));
        Exp<Func<A, B>> FromInteger(int k)
            => Lam<A, B>(x => FromInteger(k));
        Exp<Func<A, B>> FromRational(Ratio<int> k)
             => Lam<A, B>(x => FromRational(k));
        Exp<Func<A, B>> operator /(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
             => Lam<A, B>(x => f.Apply(x) / g.Apply(x));
        // End TODO

        Exp<Func<A, B>> Pi() => Lam<A, B>(x => Pi());
        Exp<Func<A, B>> Sqrt(Exp<Func<A, B>> f) => Lam<A, B>(x => Sqrt(f.Apply(x)));
        Exp<Func<A, B>> Exp(Exp<Func<A, B>> f) => Lam<A, B>(x => Exp(f.Apply(x)));
        Exp<Func<A, B>> Log(Exp<Func<A, B>> f) => Lam<A, B>(x => Log(f.Apply(x)));
        Exp<Func<A, B>> Pow(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => Pow(f.Apply(x), g.Apply(x)));
        Exp<Func<A, B>> LogBase(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => LogBase(f.Apply(x), g.Apply(x)));

        Exp<Func<A, B>> Sin(Exp<Func<A, B>> f) => Lam<A, B>(x => Sin(f.Apply(x)));
        Exp<Func<A, B>> Cos(Exp<Func<A, B>> f) => Lam<A, B>(x => Cos(f.Apply(x)));
        Exp<Func<A, B>> Tan(Exp<Func<A, B>> f) => Lam<A, B>(x => Tan(f.Apply(x)));
        Exp<Func<A, B>> Asin(Exp<Func<A, B>> f) => Lam<A, B>(x => Asin(f.Apply(x)));
        Exp<Func<A, B>> Acos(Exp<Func<A, B>> f) => Lam<A, B>(x => Acos(f.Apply(x)));
        Exp<Func<A, B>> Atan(Exp<Func<A, B>> f) => Lam<A, B>(x => Atan(f.Apply(x)));
        Exp<Func<A, B>> Sinh(Exp<Func<A, B>> f) => Lam<A, B>(x => Sinh(f.Apply(x)));
        Exp<Func<A, B>> Cosh(Exp<Func<A, B>> f) => Lam<A, B>(x => Cosh(f.Apply(x)));
        Exp<Func<A, B>> Tanh(Exp<Func<A, B>> f) => Lam<A, B>(x => Tanh(f.Apply(x)));
        Exp<Func<A, B>> Asinh(Exp<Func<A, B>> f) => Lam<A, B>(x => Asinh(f.Apply(x)));
        Exp<Func<A, B>> Acosh(Exp<Func<A, B>> f) => Lam<A, B>(x => Acosh(f.Apply(x)));
        Exp<Func<A, B>> Atanh(Exp<Func<A, B>> f) => Lam<A, B>(x => Atanh(f.Apply(x)));
    }
}
