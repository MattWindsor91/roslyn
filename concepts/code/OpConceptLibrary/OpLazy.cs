using System.Concepts.OpPrelude;

/// <summary>
///     Prelude instances for <see cref="System.Lazy{T}"/>.
/// </summary>
namespace System.Concepts.OpLazy
{
    instance LazyEq<T, implicit EqT> : Eq<Lazy<T>> where EqT : Eq<T>
    {
        bool operator ==(Lazy<T> a, Lazy<T> b) => a.Value == b.Value;
        bool operator !=(Lazy<T> a, Lazy<T> b) => a.Value != b.Value;
    }

    instance LazyOrd<T, implicit OrdT> : Ord<Lazy<T>> where OrdT : Ord<T>
    {
        bool operator ==(Lazy<T> a, Lazy<T> b) => a.Value == b.Value;
        bool operator !=(Lazy<T> a, Lazy<T> b) => a.Value != b.Value;
        bool operator >=(Lazy<T> a, Lazy<T> b) => a.Value >= b.Value;
        bool operator <=(Lazy<T> a, Lazy<T> b) => a.Value <= b.Value;
    }

    instance LazyNum<T, implicit NumT> : Num<Lazy<T>> where NumT: Num<T>
    {
        Lazy<T> operator +(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value + b.Value);
        Lazy<T> operator -(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value - b.Value);
        Lazy<T> operator *(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value * b.Value);
        Lazy<T> Abs(Lazy<T> a)                   => new Lazy<T>(() => Abs(a.Value));
        Lazy<T> Signum(Lazy<T> a)                => new Lazy<T>(() => Signum(a.Value));
        Lazy<T> FromInteger(int a)               => new Lazy<T>(() => FromInteger(a));
    }

    instance LazyFractional<T, implicit FractionalT> : Fractional<Lazy<T>> where FractionalT : Fractional<T>
    {
        Lazy<T> operator +(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value + b.Value);
        Lazy<T> operator -(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value - b.Value);
        Lazy<T> operator *(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value * b.Value);
        Lazy<T> Abs(Lazy<T> a)                   => new Lazy<T>(() => Abs(a.Value));
        Lazy<T> Signum(Lazy<T> a)                => new Lazy<T>(() => Signum(a.Value));
        Lazy<T> FromInteger(int a)               => new Lazy<T>(() => FromInteger(a));
        Lazy<T> operator /(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value / b.Value);
        Lazy<T> FromRational(Ratio<int> a)       => new Lazy<T>(() => FromRational(a));
    }

    instance LazyFloating<T, implicit FloatingT> : Floating<Lazy<T>> where FloatingT : Floating<T>
    {
        Lazy<T> operator +(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value + b.Value);
        Lazy<T> operator -(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value - b.Value);
        Lazy<T> operator *(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value * b.Value);
        Lazy<T> Abs(Lazy<T> a)                   => new Lazy<T>(() => Abs(a.Value));
        Lazy<T> Signum(Lazy<T> a)                => new Lazy<T>(() => Signum(a.Value));
        Lazy<T> FromInteger(int a)               => new Lazy<T>(() => FromInteger(a));
        Lazy<T> operator /(Lazy<T> a, Lazy<T> b) => new Lazy<T>(() => a.Value / b.Value);
        Lazy<T> FromRational(Ratio<int> a)       => new Lazy<T>(() => FromRational(a));
        Lazy<T> Pi()                             => new Lazy<T>(() => Pi());
        Lazy<T> Exp(Lazy<T> a)                   => new Lazy<T>(() => Exp(a.Value));
        Lazy<T> Log(Lazy<T> a)                   => new Lazy<T>(() => Log(a.Value));
        Lazy<T> Pow(Lazy<T> a, Lazy<T> b)        => new Lazy<T>(() => Pow(a.Value, b.Value));
        Lazy<T> LogBase(Lazy<T> a, Lazy<T> b)    => new Lazy<T>(() => LogBase(a.Value, b.Value));
        Lazy<T> Sqrt(Lazy<T> a)                  => new Lazy<T>(() => Sqrt(a.Value));
        Lazy<T> Sin(Lazy<T> a)                   => new Lazy<T>(() => Sin(a.Value));
        Lazy<T> Cos(Lazy<T> a)                   => new Lazy<T>(() => Cos(a.Value));
        Lazy<T> Tan(Lazy<T> a)                   => new Lazy<T>(() => Tan(a.Value));
        Lazy<T> Asin(Lazy<T> a)                  => new Lazy<T>(() => Asin(a.Value));
        Lazy<T> Acos(Lazy<T> a)                  => new Lazy<T>(() => Acos(a.Value));
        Lazy<T> Atan(Lazy<T> a)                  => new Lazy<T>(() => Atan(a.Value));
        Lazy<T> Sinh(Lazy<T> a)                  => new Lazy<T>(() => Sinh(a.Value));
        Lazy<T> Cosh(Lazy<T> a)                  => new Lazy<T>(() => Cosh(a.Value));
        Lazy<T> Tanh(Lazy<T> a)                  => new Lazy<T>(() => Tanh(a.Value));
        Lazy<T> Asinh(Lazy<T> a)                 => new Lazy<T>(() => Asinh(a.Value));
        Lazy<T> Acosh(Lazy<T> a)                 => new Lazy<T>(() => Acosh(a.Value));
        Lazy<T> Atanh(Lazy<T> a)                 => new Lazy<T>(() => Atanh(a.Value));
    }
}