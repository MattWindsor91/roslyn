using System.Concepts.OpPrelude;
using System.Numerics;

/// <summary>
///     Prelude instances for <see cref="System.Numerics" /> classes.
/// </summary>
namespace System.Concepts.OpNumerics
{
    public instance CIVector2 : Eq<Vector2>, Num<Vector2>, Fractional<Vector2>
    {
        bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);
        bool operator !=(Vector2 a, Vector2 b) => !(a.Equals(b));
 
        Vector2 operator +(Vector2 a, Vector2 b) => Vector2.Add(a, b);
        Vector2 operator -(Vector2 a, Vector2 b) => Vector2.Subtract(a, b);
        Vector2 operator *(Vector2 a, Vector2 b) => Vector2.Multiply(a, b);
        Vector2 Abs(Vector2 a) => Vector2.Abs(a);
        // This is contrived.
        Vector2 Signum(Vector2 a) => new Vector2(Math.Sign(a.X), Math.Sign(a.Y));
        Vector2 FromInteger(int a) => new Vector2(a);
        Vector2 operator /(Vector2 a, Vector2 b) => Vector2.Divide(a, b);
        Vector2 FromRational(Ratio<int> a) => new Vector2(a.num / a.den);
    }
}