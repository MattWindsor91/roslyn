using System;
using System.Concepts.Prelude;

using ExpressionUtils;
using static ExpressionUtils.Utils;
/// <summary>
///     Numeric tower instances for functions.
/// </summary>
namespace BeautifulDifferentiation.ExpInstances
{
    instance NumExp : Num<Exp<double>>
    {
        Exp<double> Add(Exp<double> e1, Exp<double> e2) =>
            Prim((d1, d2) => d1 + d2, e1, e2);
        Exp<double> Sub(Exp<double> e1, Exp<double> e2) =>
            Prim((d1, d2) => d1 - d2, e1, e2);
        Exp<double> Mul(Exp<double> e1, Exp<double> e2) =>
            Prim((d1, d2) => d1 * d2, e1, e2);
        Exp<double> FromInteger(int i) =>
           new Constant<double>(i);
        Exp<double> Signum(Exp<double> e1) => //TBR
           Prim(d => (double)Math.Sign(d), e1);
        Exp<double> Abs(Exp<double> e1) =>
           Prim(d => Math.Abs(d), e1);
    }

    public instance ExpDouble : Floating<Exp<double>>
    {
        //
        // Num (via Floating)
        //
        Exp<double> Add(Exp<double> x, Exp<double> y) =>
               Prim((d1, d2) => d1 + d2, x, y);
        Exp<double> Sub(Exp<double> x, Exp<double> y) =>
               Prim((d1, d2) => d1 - d2, x, y);
        Exp<double> Mul(Exp<double> x, Exp<double> y) =>
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
        Exp<double> Div(Exp<double> x, Exp<double> y) =>
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
        Exp<Func<A, B>> Add(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>((x) => Add(f.Apply(x), g.Apply(x)));
        Exp<Func<A, B>> Sub(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => Sub(f.Apply(x), g.Apply(x)));
        Exp<Func<A, B>> Mul(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => Mul(f.Apply(x), g.Apply(x)));
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
        Exp<Func<A, B>> Add(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
             => NumF<A, B>.Add(f, g);
        Exp<Func<A, B>> Sub(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => NumF<A, B>.Sub(f, g);
        Exp<Func<A, B>> Mul(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => NumF<A, B>.Mul(f, g);
        Exp<Func<A, B>> Abs(Exp<Func<A, B>> f) => NumF<A, B>.Abs(f);
        Exp<Func<A, B>> Signum(Exp<Func<A, B>> f) => NumF<A, B>.Signum(f);
        Exp<Func<A, B>> FromInteger(int k) => NumF<A, B>.FromInteger(k);

        Exp<Func<A, B>> FromRational(Ratio<int> k)
             => Lam<A, B>(x => FromRational(k));
        Exp<Func<A, B>> Div(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
             => Lam<A, B>(x => Div(f.Apply(x), g.Apply(x)));
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
        Exp<Func<A, B>> Add(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => FracF<A, B>.Add(f, g);
        Exp<Func<A, B>> Sub(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => FracF<A, B>.Sub(f, g);
        Exp<Func<A, B>> Mul(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => FracF<A, B>.Mul(f, g);
        Exp<Func<A, B>> Abs(Exp<Func<A, B>> f) => FracF<A, B>.Abs(f);
        Exp<Func<A, B>> Signum(Exp<Func<A, B>> f) => FracF<A, B>.Signum(f);
        Exp<Func<A, B>> FromInteger(int k) => FracF<A, B>.FromInteger(k);
        Exp<Func<A, B>> FromRational(Ratio<int> k) => FracF<A, B>.FromRational(k);
        Exp<Func<A, B>> Div(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => FracF<A, B>.Div(f, g);

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