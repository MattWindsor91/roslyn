# _Concept-C#_

We've forked [Roslyn][] to add _concepts_: superpowered interfaces for
C# extension methods, inspired by [Haskell typeclasses][], [Scala implicits][],
and [Rust traits][].  Concepts are brought to you by [@crusso][] and
[@MattWindsor91][].

Here's a simple example of concepts:

    public concept CMonoid<T>
    {
        T Append(this T x, T y);  // extension methods
        T Zero { get; }           // properties on types!
    }

    public instance Monoid_Ints : CMonoid<int>
    {
        int Append(this int x, int y) => x + y;
        int Zero => 0;
    }

    public static T Sum<T, implicit M>(T[] ts) where M : CMonoid<T>
    {
        T acc = M.Zero;
        foreach (var t in ts) acc = acc.Append(t);
        return acc;
    }

    Sum(new int[] { 1, 2, 3, 4, 5 });  // 15

For a deeper introduction to concepts, see `concepts\docs\tour.md`.

[Roslyn]:                 https://github.com/dotnet/roslyn
[Haskell typeclasses]:    https://www.haskell.org/tutorial/classes.html
[Scala implicits]:        http://docs.scala-lang.org/tour/implicit-parameters.html
[Rust traits]:            https://doc.rust-lang.org/book/second-edition/ch10-02-traits.html
[@crusso]:                https://github.com/crusso
[@MattWindsor91]:         https://github.com/MattWindsor91

## Health Warnings

**This is an experimental prototype**.  It _will_ behave oddly, crash, eat your
laundry, set fire to your cats, and otherwise ruin your day.  In particular,
Visual Basic doesn't support concepts, and probably doesn't work at all.

**Do _not_ report bugs in this fork to the `dotnet/Roslyn` team,** unless you
can reproduce them on normal Roslyn too.

## How to Build

The build process is the same as [Roslyn's build process][], so we recommend
you follow their existing documentation.

[Roslyn's build process]: https://github.com/dotnet/roslyn/wiki/Building%20Testing%20and%20Debugging

## How to Test

If you have Visual Studio, the easiest way to test Concept-C# is to use
Roslyn's `CompilerExtension` and `VisualStudioSetup.Next` projects to install
the Concept-C# versions of Roslyn internals into a testbed Visual Studio
instance.

Otherwise, you can use Concept-C#'s `csc.exe` to compile programs with concepts.
It can be slotted into `msbuild` using the `/p:CscToolPath` switch, for
instance.

### Warning

To use concepts, you must build and reference `ConceptAttributes.dll`. The
project for this is located in `concepts\code\ConceptAttributes`.

## Examples

We have a large set of ready-to-build examples in the
`concepts\code\Concepts.sln` solution.  These include:

* `\ConceptLibrary\`: a standard library for concepts, including a Haskell-style
  prelude and concepts for enumerating and converting to string;
* `\Features\`: small testbeds for concept features;
* `\BeautifulDifferentiation\`: an implementation of Conal Elliott's
  _[Beautiful Differentiation][]_ paper, using concepts for numeric abstraction;
* `\Quickhull\`: an implementation of the _[Quickhull][]_ algorithm, using
  concepts for monoids and numeric abstraction;
* `\SerialPBT\`: a simple property-based tester similar to Runciman et al.'s
  _[SmallCheck][]_, using concepts to abstract over testable properties;
* `\TinyLINQ\`: a prototype concept-based implementation of most of
  _[LINQ to Objects][]_.

[Beautiful Differentiation]: http://conal.net/papers/beautiful-differentiation/beautiful-differentiation.pdf
[LINQ to Objects]:          https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/linq-to-objects
[Quickhull]:                https://en.wikipedia.org/wiki/Quickhull
[SmallCheck]:               https://www.cs.york.ac.uk/fp/smallcheck/smallcheck.pdf

## Documentation

Supporting documentation is included in the `concepts\docs` directory.
This is usually in [Pandoc][]-compatible Markdown-with-LaTeX.

Our general design (based on implementation ideas by Claudio Russo)
is included in `concepts\docs\concepts.md`.

[Pandoc]: https://pandoc.org

## Further reading

* The _[shapes proposal][]_ was based on an earlier version of Concept-C#, and
  recently we've been working to try align our feature set to it.
  There's a comparison between what we do and what Shapes does in
  `concepts\code\ShapesComparison\`.

[shapes proposal]: https://github.com/dotnet/csharplang/issues/164