/// <summary>
///     Prelude of common concepts.
/// </summary>
/// <remarks>
///     This prelude is heavily inspired by that of Haskell 98, but
///     implemented directly in terms of C#'s standard library where
///     possible.
/// </remarks>
namespace System.Concepts.Prelude
{
    #region Eq

    /// <summary>
    ///     Concept for equality.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which equality is being defined.
    /// </typeparam>
    /// <remarks>
    ///     Minimal complete definition is either
    ///     <see cref="Equals"/> or <see cref="NotEquals"/>.
    /// </remarks>
    public concept Eq<A>
    {
        /// <summary>
        ///     Returns true iff <paramref name="x"/> equals
        ///     <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The first item to compare for equality.
        /// </param>
        /// <param name="y">
        ///     The second item to compare for equality.
        /// </param>
        /// <returns>
        ///     True iff <paramref name="a"/> equals
        ///     <paramref name="y"/>: <c>x == y</c>.
        /// </returns>
        /// <remarks>
        ///     If this is not defined, the default is
        ///     to take the negation of <see cref="NotEquals"/>.
        /// </remarks>
        bool Equals(A x, A y) => !NotEquals(x, y);

        /// <summary>
        ///     Returns false iff <paramref name="x"/> equals
        ///     <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The first item to compare for disequality.
        /// </param>
        /// <param name="y">
        ///     The second item to compare for disequality.
        /// </param>
        /// <returns>
        ///     False iff <paramref name="a"/> equals
        ///     <paramref name="y"/>: <c>x != y</c>.
        /// </returns>
        /// <remarks>
        ///     If this is not defined, the default is
        ///     to take the negation of <see cref="Equals"/>.
        /// </remarks>
        bool NotEquals(A x, A y) => !Equals(x, y);
    }

    // Subconcept implementations of Eq:
    // - Eq<bool>   -> PreludeBool.
    // - Eq<int>    -> PreludeInt.
    // - Eq<double> -> PreludeDouble.

    /// <summary>
    ///     Implementation of <see cref="Eq{A}"/> for arrays.
    /// </summary>
    public instance EqArray<A, implicit EqA> : Eq<A[]> where EqA: Eq<A>
    {
        bool Equals(A[] x, A[] y)
        {
            if (x == null) return y == null;
            if (y == null) return false;
            if (x.Length != y.Length) return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (!EqA.Equals(x[i], y[i])) return false;
            }
            return true;
        }
    }

    #endregion Eq


    #region Ord

    /// <summary>
    ///     Concept for total ordering.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which ordering is being defined.
    /// </typeparam>
    /// <remarks>
    ///    Minimal complete definition is <see cref="Leq"/>.
    /// </remarks>
    public concept Ord<A> : Eq<A>
    {
        // TODO: compare

        /// <summary>
        ///     Returns true if <paramref name="x"/> is less than or
        ///     equal to <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The first item to compare.
        /// </param>
        /// <param name="y">
        ///     The second item to compare.
        /// </param>
        /// <returns>
        ///     True if <paramref name="x"/> is less than or equal to
        ///     <paramref name="y"/>: <c>x <= y</c>.
        /// </returns>
        bool Leq(A x, A y);

        /// <summary>
        ///     Returns true if <paramref name="x"/> is less than
        ///     <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The first item to compare.
        /// </param>
        /// <param name="y">
        ///     The second item to compare.
        /// </param>
        /// <returns>
        ///     True if <paramref name="x"/> is less than
        ///     <paramref name="y"/>: <c>x < y</c>.
        /// </returns>
        bool Lt(A x, A y) => Leq(x, y) && !NotEquals(x, y);

    }

    // Subconcept implementations of Ord:
    // - Ord<bool>   -> PreludeBool.
    // - Ord<int>    -> PreludeInt.
    // - Ord<double> -> PreludeDouble
    #endregion Ord


    #region Num

    /// <summary>
    ///     Concept for basic numbers.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which numeric operations are being defined.
    /// </typeparam>
    /// <remarks>
    ///     Minimal complete definition is all except either
    ///     <see cref="Sub"/> or <see cref="Neg"/>.
    /// </remarks>
    public concept Num<A>
    {
        /// <summary>
        ///     Adds <paramref name="y"/> to <paramref name="x"/>.
        /// </summary>
        /// <param name="x">
        ///     The augend to which <paramref name="y"/> is added.
        /// </param>
        /// <param name="y">
        ///     The addend to be added to <paramref name="x"/>.
        /// </param>
        /// <returns>
        ///     The result of adding <paramref name="y"/> to
        ///     <paramref name="x"/>: <c>x + y</c>.
        /// </returns>
        A Add(A x, A y);

        /// <summary>
        ///     Subtracts <paramref name="y"/> from
        ///     <paramref name="x"/>.
        /// </summary>
        /// <param name="x">
        ///     The minuend from which <paramref name="y"/> is
        ///     subtracted.
        /// </param>
        /// <param name="y">
        ///     The subtrahend to be taken from <paramref name="x"/>.
        /// </param>
        /// <returns>
        ///     The result of subtracting <paramref name="y"/> from
        ///     <paramref name="x"/>: <c>x - y</c>.
        /// </returns>
        A Sub(A x, A y) => Add(x, Neg(y));


        /// <summary>
        ///     Negates <paramref name="x"/>.
        /// </summary>
        /// <param name="x">
        ///     The quantity to negate.
        /// </param>
        /// <returns>
        ///     The result of negating <paramref name="x"/>: <c>-x</c>.
        /// </returns>
        /// <remarks>
        ///     If not defined, the default is to subtract
        ///     <paramref name="x"/> from zero.
        /// </remarks>
        A Neg(A x) => Sub(FromInteger(0), x);

        // Haskell here allows either (-) or negate to be defined: we
        // currently just define Sub.

        /// <summary>
        ///     Multiplies <paramref name="x"/> and
        ///     <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The multiplier by which <paramref name="y"/> is
        ///     multiplied.
        /// </param>
        /// <param name="y">
        ///     The multiplicand to be multiplied by
        ///     <paramref name="x"/>.
        /// </param>
        /// <returns>
        ///     The result of multiplying <paramref name="x"/> and
        ///     <paramref name="y"/>: <c>x * y</c>.
        /// </returns>
        A Mul(A x, A y);

        /// <summary>
        ///     Takes the absolute value of <paramref name="x"/>.
        /// </summary>
        /// <param name="x">
        ///     A value, the absolute value of which is to be taken.
        /// </param>
        /// <returns>
        ///     The absolute value of <paramref name="x"/>, such that
        ///     <c>Mul(Abs(x), Signum(x)) == x</c>.
        /// </returns>
        A Abs(A x);

        /// <summary>
        ///     Takes the sign of <paramref name="x"/>.
        /// </summary>
        /// <param name="x">
        ///     A value, the sign of which is to be taken.
        /// </param>
        /// <returns>
        ///     The sign of <paramref name="x"/>, such that
        ///     <c>Mul(Abs(x), Signum(x)) == x</c>.  Usually, this will
        ///     be <c>-1</c> for negative values, <c>0</c> for zero,
        ///     and <c>1</c> for positive values.
        /// </returns>
        A Signum(A x);

        /// <summary>
        ///     Generates a numeric value from an integer.
        /// </summary>
        /// <param name="x">
        ///     The integer to convert.
        /// </param>
        /// <returns>
        ///     The equivalent of <paramref name="x"/> in the
        ///     implementing type.
        /// </returns>
        A FromInteger(int x);

        // Haskell uses arbitrary-precision integers here, so maybe we
        // should do too?
    }

    // Subconcept implementations of Num:
    // - Num<int>    -> PreludeInt
    // - Num<double> -> PreludeDouble

    #endregion Num


    #region Fractional

    /// <summary>
    ///     A ratio between two values.
    /// </summary>
    public struct Ratio<A>
    {
        // This is used in the definition of Fractional.

        /// <summary>
        ///     The numerator of the ratio, in non-reduced form.
        /// </summary>
        public A num;

        /// <summary>
        ///     The denominator of the ratio, in non-reduced form.
        /// </summary>
        public A den;
    }

    /// <summary>
    ///     Concept for fractional numbers.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which fractional operations are being defined.
    /// </typeparam>
    /// <remarks>
    ///     Minimum complete definition:
    ///     <see cref="FromRational"/> and either <see cref="Recip"/> or
    ///     <see cref="Div"/>.
    /// </remarks>
    public concept Fractional<A> : Num<A>
    {
        /// <summary>
        ///     Fractionally divides <paramref name="x"/> by
        ///     <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The dividend to be divided by <paramref name="y"/>.
        /// </param>
        /// <param name="y">
        ///     The divisor to divide <paramref name="x"/>.
        /// </param>
        /// <returns>
        ///     The result of fractionally dividing <paramref name="x"/>
        ///     by <paramref name="y"/>: <c>x / y</c>.
        /// </returns>
        /// <remarks>
        ///     If not defined, the default is to take the multiplication of
        ///     <paramref name="x"/> by the reciprocal of <paramref name="y"/>.
        /// </remarks>
        A Div(A x, A y) => Mul(x, Recip(y));

        /// <summary>
        ///     Takes the reciprocal of <paramref name="x"/>.
        /// </summary>
        /// <param name="x">
        ///     The value to be reciprocated.
        /// </param>
        /// <returns>
        ///     The reciprocal of <paramref name="x"/>.
        /// </returns>
        /// <remarks>
        ///     If not defined, the default is to divide 1 by
        ///     <paramref name="x"/>.
        /// </remarks>
        A Recip(A x) => Div(FromInteger(1), x);

        // Haskell also allows the reciprocal to be defined, but, for
        // now, we don't.

        /// <summary>
        ///     Generates a fractional value from an integer ratio.
        /// </summary>
        /// <param name="x">
        ///     The ratio to convert.
        /// </param>
        /// <returns>
        ///     The equivalent of <paramref name="x"/> in the
        ///     implementing type.
        /// </returns>
        A FromRational(Ratio<int> x);

        // Haskell uses arbitrary-precision integers here, so maybe we
        // should do to?
    }

    // Subconcept implementations of Fractional:
    // - Fractional<double> -> PreludeDouble

    #endregion Fractional


    #region Floating

    /// <summary>
    ///     Concept for floating-point numbers.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which floating operations are being defined.
    /// </typeparam>
    /// <remarks>
    ///     The minimum complete definition is
    ///     <see cref="pi"/>,
    ///     <see cref="exp"/>,
    ///     <see cref="log"/>,
    ///     <see cref="sin"/>,
    ///     <see cref="cos"/>,
    ///     <see cref="sinh"/>,
    ///     <see cref="sinh"/>,
    ///     <see cref="cosh"/>,
    ///     <see cref="asin"/>,
    ///     <see cref="acos"/>,
    ///     <see cref="atan"/>,
    ///     <see cref="asinh"/>,
    ///     <see cref="acosh"/>, and
    ///     <see cref="atanh"/>.
    ///  </remarks>
    public concept Floating<A> : Fractional<A>
    {
        /// <summary>
        ///     Returns an approximation of pi.
        /// </summary>
        /// <returns>
        ///     A reasonable approximation of pi in this type.
        /// </returns>
        A Pi();

        /// <summary>
        ///     The exponential function.
        /// </summary>
        /// <param name="x">
        ///     The value for which we are calculating the exponential.
        /// </param>
        /// <returns>
        ///     The value of <c>e^x</c>.
        /// </returns>
        A Exp(A x);

        /// <summary>
        ///     Calculates the square root of a value.
        /// </summary>
        /// <param name="x">
        ///     The value for which we are calculating the square root.
        /// </param>
        /// <returns>
        ///     The value of <c>sqrt(x)</c>.
        /// </returns>
        A Sqrt(A x);

        /// <summary>
        ///     Calculates the natural logarithm of a value.
        /// </summary>
        /// <param name="x">
        ///     The value for which we are calculating the natural
        ///     logarithm.
        /// </param>
        /// <returns>
        ///     The value of <c>ln(x)</c>.
        /// </returns>
        A Log(A x);

        /// <summary>
        ///     Raises <paramref name="x"/> to the power
        ///     <paramref name="y"/>.
        /// </summary>
        /// <param name="x">
        ///     The base to be raised to <paramref name="y"/>.
        /// </param>
        /// <param name="y">
        ///     The exponent to which we are raising
        ///     <paramref name="x"/>.
        /// </param>
        /// <returns>
        ///     The value of <c>x^y</c>.
        /// </returns>
        /// <remarks>
        ///     If not defined, the default is defined in terms of the natural
        ///     logarithm and exponential function.
        /// </remarks>
        A Pow(A x, A y) => Exp(Mul(Log(x), y));

        /// <summary>
        ///     Calculates the logarithm of a value with respect to an
        ///     arbitrary base.
        /// </summary>
        /// <param name="b">
        ///     The base to use for the logarithm of
        ///     <paramref name="x"/>.
        /// </param>
        /// <param name="x">
        ///     The value for which we are calculating the logarithm.
        /// </param>
        /// <returns>
        ///     The value of <c>log b (x)</c>.
        /// </returns>
        /// <remarks>
        ///     If not defined, the default is defined in terms of the natural
        ///     logarithm.
        /// </remarks>
        A LogBase(A b, A x) => Div(Log(x), Log(b));

        /// <summary>
        ///     Calculates the sine of an angle.
        /// </summary>
        /// <param name="x">
        ///     An angle, in radians.
        /// </param>
        /// <returns>
        ///     The value of <c>sin(x)</c>.
        /// </returns>
        A Sin(A x);

        /// <summary>
        ///     Calculates the cosine of an angle.
        /// </summary>
        /// <param name="x">
        ///     An angle, in radians.
        /// </param>
        /// <returns>
        ///     The value of <c>cos(x)</c>.
        /// </returns>
        A Cos(A x);

        /// <summary>
        ///     Calculates the tangent of an angle.
        /// </summary>
        /// <param name="x">
        ///     An angle, in radians.
        /// </param>
        /// <returns>
        ///     The value of <c>tan(x)</c>.
        /// </returns>
        /// <remarks>
        ///     If left undefined, the default is sine over cosine.
        /// </remarks>
        A Tan(A x) => Div(Sin(x), Cos(x));

        /// <summary>
        ///     Calculates an arcsine.
        /// </summary>
        /// <param name="x">
        ///     The value for which an arcsine is to be calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>asin(x)</c>, in radians.
        /// </returns>
        A Asin(A x);

        /// <summary>
        ///     Calculates an arc-cosine.
        /// </summary>
        /// <param name="x">
        ///     The value for which an arc-cosine is to be calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>acos(x)</c>, in radians.
        /// </returns>
        A Acos(A x);

        /// <summary>
        ///     Calculates an arc-tangent.
        /// </summary>
        /// <param name="x">
        ///     The value for which an arc-tangent is to be calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>atan(x)</c>, in radians.
        /// </returns>
        A Atan(A x);

        /// <summary>
        ///     Calculates a hyperbolic sine.
        /// </summary>
        /// <param name="x">
        ///     The value for which a hyperbolic sine
        ///     is to be calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>sinh(x)</c>.
        /// </returns>
        A Sinh(A x);

        /// <summary>
        ///     Calculates a hyperbolic cosine.
        /// </summary>
        /// <param name="x">
        ///     The value for which a hyperbolic cosine
        ///     is to be calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>cosh(x)</c>.
        /// </returns>
        A Cosh(A x);

        /// <summary>
        ///     Calculates a hyperbolic tangent.
        /// </summary>
        /// <param name="x">
        ///     The value for which a hyperbolic tangent
        ///     is to be calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>tanh(x)</c>.
        /// </returns>
        /// <remarks>
        ///     If left undefined, the default is sinh over cosh.
        /// </remarks>
        A Tanh(A x) => Div(Sinh(x), Cosh(x));

        /// <summary>
        ///     Calculates an area hyperbolic sine.
        /// </summary>
        /// <param name="x">
        ///     The value for which an area hyperbolic sine is to be
        ///     calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>asinh(x)</c>.
        /// </returns>
        A Asinh(A x);

        /// <summary>
        ///     Calculates an area hyperbolic cosine.
        /// </summary>
        /// <param name="x">
        ///     The value for which an area hyperbolic cosine is to be
        ///     calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>acosh(x)</c>.
        /// </returns>
        A Acosh(A x);

        /// <summary>
        ///     Calculates an area hyperbolic tangent.
        /// </summary>
        /// <param name="x">
        ///     The value for which an area hyperbolic tangent is to be
        ///     calculated.
        /// </param>
        /// <returns>
        ///     The value of <c>atanh(x)</c>.
        /// </returns>
        A Atanh(A x);
    }

    // Subconcept implementations of Floating:
    // - Floating<double> -> PreludeDouble

    #endregion Floating


    #region Ground instances

    /// <summary>
    ///     Implementation of <see cref="Ord{A}"/> for booleans.
    /// </summary>
    /// <remarks>
    ///     Order is specified by having <c>true > false</c>.
    /// </remarks>
    public instance PreludeBool : Ord<bool>
    {
        // Eq (via Ord)
        bool Equals(bool x, bool y) => x == y;

        // Ord
        bool Leq(bool x, bool y) => !x || y;
    }

    /// <summary>
    ///     Implementation of <see cref="Ord{A}"/> and
    ///     <see cref="Num{A}"/> for integers.
    /// </summary>
    public instance PreludeInt : Ord<int>, Num<int>
    {
        //
        // Eq (via Ord)
        //
        bool Equals(int x, int y) => x == y;

        //
        // Ord
        //
        bool Leq(int x, int y) => x <= y;

        //
        // Num
        //
        int Add(int x, int y)  => x + y;
        int Sub(int x, int y)  => x - y;
        int Mul(int x, int y)  => x * y;
        int Abs(int x)         => Math.Abs(x);
        int Signum(int x)      => Math.Sign(x);
        int FromInteger(int x) => x;
    }

    /// <summary>
    ///     Implementation of <see cref="Ord{A}"/> and
    ///     <see cref="Floating{A}"/> for doubles.
    /// </summary>
    public instance PreludeDouble : Ord<double>, Floating<double>
    {
        //
        // Eq (via Ord)
        //
        bool Equals(double x, double y) => x == y;

        //
        // Ord
        //
        bool Leq(double x, double y) => x <= y;

        //
        // Num (via Floating)
        //
        double Add(double x, double y)     => x + y;
        double Sub(double x, double y)     => x - y;
        double Mul(double x, double y)     => x * y;
        double Abs(double x)               => Math.Abs(x);
        double Signum(double x)            => Math.Sign(x);
        double FromInteger(int x)          => (double)x;

        //
        // Fractional (via Floating)
        //
        double Div(double x, double y)     => x / y;
        double FromRational(Ratio<int> x)  => x.num / x.den;

        //
        // Floating
        //
        double Pi()                        => Math.PI;
        double Exp(double x)               => Math.Exp(x);
        double Sqrt(double x)              => Math.Sqrt(x);
        double Log(double x)               => Math.Log(x);
        double Pow(double x, double y)     => Math.Pow(x, y);
        // Haskell and C# put the base in different places.
        // Maybe we should adopt the C# version?
        double LogBase(double b, double x) => Math.Log(x, b);
        double Sin(double x)               => Math.Sin(x);
        double Cos(double x)               => Math.Cos(x);
        double Tan(double x)               => Math.Tan(x);
        double Asin(double x)              => Math.Asin(x);
        double Acos(double x)              => Math.Acos(x);
        double Atan(double x)              => Math.Atan(x);
        double Sinh(double x)              => Math.Sinh(x);
        double Cosh(double x)              => Math.Cosh(x);
        double Tanh(double x)              => Math.Tanh(x);
        // Math doesn't have these, so define them directly in terms of
        // logarithms.
        double Asinh(double x)
            => Math.Log(x + Math.Sqrt((x * x) + 1.0));
        double Acosh(double x)
            => Math.Log(x + Math.Sqrt((x * x) - 1.0));
        double Atanh(double x)
            => 0.5 * Math.Log((1.0 + x) / (1.0 - x));
    }

    #endregion Ground instances
}