using System;
using System.Collections.Generic;
using System.Drawing;
using System.Concepts.OpPrelude;
using static System.Concepts.OpPrelude.Verbose;
using System.Concepts.OpMonoid;
using System.Linq;
using static System.Concepts.OpMonoid.Utils;
using static Utils;

/// <summary>
///     Concept for numbers convertible to single-precision floats.
/// </summary>
/// <typeparam name="A">
///    The type to convert.
/// </typeparam>
public concept ToSingle<A>
{
    /// <summary>
    ///     Converts a number to an Single.
    /// </summary>
    /// <param name="x">
    ///     The number to convert.
    /// </param>
    /// <returns>
    ///     The number as an Single.
    /// </returns>
    Single ToSingle(A x);
}

/// <summary>
///     Instance of ToSingle for single-precision floats..
/// </summary>
public instance ToSingleFloat : ToSingle<float>
{
    /// <summary>
    ///     Converts a number to an Single.
    /// </summary>
    /// <param name="x">
    ///     The number to convert.
    /// </param>
    /// <returns>
    ///     The number as an Single.
    /// </returns>
    float ToSingle(float x) => x;
}

/// <summary>
///     Instance of ToSingle for double-precision floats..
/// </summary>
public instance ToSingleDouble : ToSingle<double>
{
    /// <summary>
    ///     Converts a number to an Single.
    /// </summary>
    /// <param name="x">
    ///     The number to convert.
    /// </param>
    /// <returns>
    ///     The number as an Single.
    /// </returns>
    float ToSingle(double x) => (float)x;
}

/// <summary>
///     A two-dimensional point, generic over any numeric type.
/// </summary>
/// <typeparam name="A">
///     The underlying numeric type.
/// </typeparam>
public struct Point<A>
{
    /// <summary>
    ///     Gets or sets the X co-ordinate of this Point.
    /// </summary>
    public A X { get; set; }

    /// <summary>
    ///     Gets or sets the Y co-ordinate of this Point.
    /// </summary>
    public A Y { get; set; }
}

/// <summary>
///     A line segment between two points.
/// </summary>
/// <typeparam name="A">
///     The underlying numeric type.
/// </typeparam>
public struct Line<A>
{
    /// <summary>
    ///     Gets or sets the first point on this line.
    /// </summary>
    public Point<A> P1 { get; set; }

    /// <summary>
    ///     Gets or sets the second point on this line.
    /// </summary>
    public Point<A> P2 { get; set; }

    /// <summary>
    ///     Flips the points on this line.
    /// </summary>
    /// <returns>
    ///     A new line with the points flipped.
    /// </returns>
    public Line<A> Flip()
    {
        return new Line<A> { P2 = this.P1, P1 = this.P2 };
    }

    /// <summary>
    ///     Decides whether a point is on the right of this line.
    /// </summary>
    /// <param name="point">
    ///     The point to consider.
    /// </param>
    /// <returns>
    ///     True if the point is on the right of this line, including
    ///     points on the line.
    /// </returns>
    public bool OnRight<implicit OrdA, implicit NumA>(Point<A> point)
        where OrdA : Ord<A>
        where NumA : Num<A>
    {
        // From http://stackoverflow.com/questions/1560492/
        return (
            FromInteger(0) <=
            Signum(
                ((P2.X - P1.X) * (point.Y - P1.Y)) -
                ((P2.Y - P1.Y) * (point.X - P1.X))
            )
        );
    }

    /// <summary>
    ///     Calculates the distance from a point to this line.
    /// </summary>
    /// <param name="point">
    ///     The point to consider.
    /// </param>
    /// <returns>
    ///     The distance from the point to this line.
    /// </returns>
    public A PointDistance<implicit OrdA, implicit FloatA>(Point<A> point)
        where OrdA : Ord<A>
        where FloatA : Floating<A>
    {
        return (
            Abs(
                (((P2.Y - P1.Y) * point.X) - ((P2.X - P1.X) * point.Y))
                + ((P2.X * P1.Y) - (P2.Y * P1.X))
            ) /
            Sqrt(
                ((P2.Y - P1.Y) * (P2.Y - P1.Y)) +
                ((P2.X - P1.X) * (P2.X - P1.X))
            )
        );
    }
}

/// <summary>
///     Ordering of points based on their X co-ordinate.
/// </summary>
instance OrdPointX<A, implicit OrdA> : Ord<Point<A>> where OrdA : Ord<A>
{
    bool operator ==(Point<A> x, Point<A> y) => Eq(x.X, y.X);
    bool operator !=(Point<A> x, Point<A> y) => Neq(x.X, y.X);
    bool operator <=(Point<A> x, Point<A> y) => Leq(x.X, y.X);
    bool operator >=(Point<A> x, Point<A> y) => Geq(x.X, y.X);
}

/// <summary>
///     Ordering of points based on their Y co-ordinate.
/// </summary>
instance OrdPointY<A, implicit OrdA> : Ord<Point<A>> where OrdA : Ord<A>
{
    bool operator ==(Point<A> x, Point<A> y) => Eq(x.Y, y.Y);
    bool operator !=(Point<A> x, Point<A> y) => Neq(x.Y, y.Y);
    bool operator <=(Point<A> x, Point<A> y) => Leq(x.Y, y.Y);
    bool operator >=(Point<A> x, Point<A> y) => Geq(x.Y, y.Y);
}

/// <summary>
///     Concept for items drawable onto a graphics context.
/// </summary>
/// <typeparam name="A">
///    The type to draw.
/// </typeparam>
public concept Drawable<A>
{
    /// <summary>
    ///     Draws the item onto a graphics context.
    /// </summary>
    /// <param name="item">
    ///     The item to draw.
    /// </param>
    /// <param name="colour">
    ///     The colour in which to draw the item.
    /// </param>
    /// <param name="gfx">
    ///     The graphics context to draw onto.
    /// </param>
    void Draw(A item, Color colour, Graphics gfx);
}

/// <summary>
///     Drawable instance for points.
/// </summary>
public instance DrawPoint<A, implicit TA> : Drawable<Point<A>>
    where TA : ToSingle<A>
{
    void Draw(Point<A> item, Color colour, Graphics gfx)
    {
        var brush = new SolidBrush(colour);
        var x = ToSingle(item.X);
        var y = ToSingle(item.Y);

        gfx.FillEllipse(brush, x - 4, y - 4, 8, 8);
    }
}

/// <summary>
///     Drawable instance for lines.
/// </summary>
public instance DrawLine<A, implicit TA> : Drawable<Line<A>>
    where TA : ToSingle<A>
{
    void Draw(Line<A> item, Color colour, Graphics gfx)
    {
        var pen = new Pen(colour, 5.0f);
        var x1 = ToSingle(item.P1.X);
        var y1 = ToSingle(item.P1.Y);
        var x2 = ToSingle(item.P2.X);
        var y2 = ToSingle(item.P2.Y);

        gfx.DrawLine(pen, x1, y1, x2, y2);
    }
}

/// <summary>
///     Composition of enumerations of drawables.
/// </summary>
public instance DrawEnum<A, implicit DA> : Drawable<IEnumerable<A>>
    where DA : Drawable<A>
{
    void Draw(IEnumerable<A> items, Color colour, Graphics gfx)
    {
        foreach (var item in items)
        {
            Draw(item, colour, gfx);
        }
    }
}

/// <summary>
///     Allows 2-tuples to be ordered by their first item.
/// </summary>
public instance Ord21<A, B, implicit OrdA> : Ord<Tuple<A, B>>
    where OrdA : Ord<A>
{
    bool operator ==(Tuple<A, B> a, Tuple<A, B> b) => Eq(a.Item1, b.Item1);
    bool operator !=(Tuple<A, B> a, Tuple<A, B> b) => Neq(a.Item1, b.Item1);
    bool operator <=(Tuple<A, B> a, Tuple<A, B> b) => Leq(a.Item1, b.Item1);
    bool operator >=(Tuple<A, B> a, Tuple<A, B> b) => Geq(a.Item1, b.Item1);
}

static class Utils
{
    /// <summary>
    ///     Computes the maximum of a non-empty list of ordered items.
    /// </summary>
    /// <param name="xs">
    ///     The list of ordered items to consider.  Must be non-empty.
    /// </param>
    /// <returns>
    ///     The maximum element of the list <paramref name="xs"/>.
    /// </returns>
    /// <typeparam name="A">
    ///     The type of the ordered elements.
    /// </typeparam>
    public static A Maximum<A, implicit OrdA>(IEnumerable<A> xs) where OrdA : Ord<A>
        => ConcatNonEmpty<A, Max<A>>(xs);

    /// <summary>
    ///     Computes the maximum of a non-empty list of ordered items
    ///     after applying a given function to them.
    /// </summary>
    /// <param name="xs">
    ///     The list of ordered items to consider.  Must be non-empty.
    /// </param>
    /// <param name="f">
    ///     The function to apply to each item of <paramref name="xs"/>
    ///     before taking the maximum.
    /// </param>
    /// <returns>
    ///     The maximum element of the list <paramref name="xs"/> after
    ///     mapping with <param name="f">.
    /// </returns>
    /// <typeparam name="A">
    ///     The type of the ordered elements after mapping..
    /// </typeparam>
    /// <typeparam name="B">
    ///     The type of the initial list.
    /// </typeparam>
    public static B MaximumBy<A, B, implicit OrdA>(IEnumerable<B> xs, Func<B, A> f)
        where OrdA : Ord<A>
        => ConcatMapNonEmpty<Tuple<A, B>, B, Max<Tuple<A, B>, Ord21<A, B>>>(
               xs, (x) => Tuple.Create(f(x), x)
           ).Item2;

    /// <summary>
    ///     Computes the maximum of a non-empty list of ordered items.
    /// </summary>
    /// <param name="xs">
    ///     The list of ordered items to consider.  Must be non-empty.
    /// </param>
    /// <returns>
    ///     The minimum element of the list <paramref name="xs"/>.
    /// </returns>
    /// <typeparam name="A">
    ///     The type of the ordered elements.
    /// </typeparam>
    public static A Minimum<A, implicit OrdA>(IEnumerable<A> xs) where OrdA : Ord<A>
        => ConcatNonEmpty<A, Min<A>>(xs);

    /// <summary>
    ///     Computes the minimum of a non-empty list of ordered items
    ///     after applying a given function to them.
    /// </summary>
    /// <param name="xs">
    ///     The list of ordered items to consider.  Must be non-empty.
    /// </param>
    /// <param name="f">
    ///     The function to apply to each item of <paramref name="xs"/>
    ///     before taking the minimum.
    /// </param>
    /// <returns>
    ///     The minimum element of the list <paramref name="xs"/> after
    ///     mapping with <param name="f">.
    /// </returns>
    /// <typeparam name="A">
    ///     The type of the ordered elements after mapping..
    /// </typeparam>
    /// <typeparam name="B">
    ///     The type of the initial list.
    /// </typeparam>
    public static B MinimumBy<A, B, implicit OrdA>(IEnumerable<B> xs, Func<B, A> f)
        where OrdA : Ord<A>
        => ConcatMapNonEmpty<Tuple<A, B>, B, Min<Tuple<A, B>, Ord21<A, B>>>(
               xs, (x) => Tuple.Create(f(x), x)
           ).Item2;
}

public class Quickhull<A>
{
    private List<Line<A>> _lines;
    private readonly Point<A>[] _points;
    private HashSet<Point<A>> _hull;

    public IEnumerable<Line<A>>  Lines  => _lines;
    public IEnumerable<Point<A>> Points => _points;
    public IEnumerable<Point<A>> Hull   => _hull;

    public Quickhull(Point<A>[] ps)
    {
        _lines  = new List<Line<A>>();
        _points = ps;
        _hull   = new HashSet<Point<A>>();
    }

    public void Recur<implicit OrdA, implicit FloatA>(Line<A> line, List<Point<A>> points)
        where OrdA : Ord<A>
        where FloatA : Floating<A>
    {
        if (points.Count == 0) return;

        var outlier = MaximumBy(points, (x) => line.PointDistance(x));
        _hull.Add(outlier);

        var ac = new Line<A> { P1 = line.P1, P2 = outlier };
        var cb = new Line<A> { P1 = outlier, P2 = line.P2 };

        _lines.Add(ac);
        _lines.Add(cb);

        var pastac = new List<Point<A>>();
        var pastcb = new List<Point<A>>();
        foreach (var point in Points)
        {
            if (_hull.Contains(point)) continue;
            if (ac.OnRight(point))
            {
                pastac.Add(point);
            }
            else if (cb.OnRight(point))
            {
                pastcb.Add(point);
            }
        }

        Recur(ac, pastac);
        Recur(cb, pastcb);
    }

    public void Run<implicit OrdA, implicit FloatA>()
        where OrdA : Ord<A>
        where FloatA : Floating<A>
    {
        _lines  = new List<Line<A>>();
        _hull   = new HashSet<Point<A>>();

        var minX = Minimum<Point<A>, OrdPointX<A>>(_points);
        var maxX = Maximum<Point<A>, OrdPointX<A>>(_points);

        _hull.Add(minX);
        _hull.Add(maxX);

        var ln = new Line<A> { P1 = minX, P2 = maxX };
        _lines.Add(ln);


        var onRight = new List<Point<A>>();
        var onLeft = new List<Point<A>>();
        foreach (var point in Points)
        {
            if (ln.OnRight(point))
            {
                onRight.Add(point);
            }
            else
            {
                onLeft.Add(point);
            }
        }

        Recur(ln, onRight);
        Recur(ln.Flip(), onLeft);
    }
}


public class QuickhullDriver
{
    private Graphics gfx;
    private Bitmap bmp;
    private int c;
    private int w;
    private int h;

    public QuickhullDriver(int width, int height, int count)
    {
        bmp = new Bitmap(width, height);
        gfx = Graphics.FromImage(bmp);
        c = count;
        w = width;
        h = height;
    }

    public Bitmap Run()
    {
        var rando = new Random();
        var pts = new Point<double>[c];

        var maxx = w - 1;
        var maxy = h - 1;

        for (int i = 0; i < c; i++)
        {
            pts[i] = new Point<double> { X = rando.Next(0, maxx), Y = rando.Next(0, maxy) };
        }

        var hull = new Quickhull<double>(pts);
        // TODO: improve inference here.
        hull.Run();

        Draw(hull.Points, Color.Green);
        Draw(hull.Lines, Color.Red);
        Draw(hull.Hull, Color.Blue);

        return bmp;
    }

    private void Draw<A, implicit DA>(A item, Color colour)
        where DA : Drawable<A>
    {
        Draw(item, colour, gfx);
    }

    public static void Main()
    {
        var bmp = new QuickhullDriver(640, 480, 100).Run();
        bmp.Save("hull.png", System.Drawing.Imaging.ImageFormat.Png);
    }
}