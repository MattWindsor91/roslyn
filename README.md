# Roslyn fork for _Concept C#_

This repository contain a fork of the [Roslyn](https://github.com/dotnet/roslyn)
compiler to add support for _concepts_ (think somewhere between Haskell
[typeclasses](https://www.haskell.org/tutorial/classes.html) and Scala
[implicits](http://www.scala-lang.org/old/node/114) to C#).  This work is
being carried out by Claudio Russo ([@crusso](https://github.com/crusso))
and Matt Windsor ([@CaptainHayashi](https://github.com/captainhayashi)).

_This is an experimental prototype_, and is _not_ suitable for production or
even for drop-in addition to the mainstream C# compiler.  Expect it to eat your
laundry, set fire to your cats, and otherwise ruin your day.

## Examples

Examples of the syntax, which may be compiled with this fork's `csc`, can be
found in the `concepts\tests` and `concepts\code` directories.

## Design overview

We outline our general design (based on implementation ideas by Claudio Russo)
in `concepts\docs\concepts.md`.

Open (and closed) design decision questions and requests for comment are hosted
[on a separate repository](https://github.com/CaptainHayashi/roslyn-concepts-issues).

## How to compile the compiler

See Roslyn's [existing documentation](https://github.com/dotnet/roslyn/wiki/Building%20Testing%20and%20Debugging)
on building, testing, and debugging: we haven't changed the build process for
the compiler.

## How to compile the examples

First cd to `concepts` and compile `ConceptAttributes.dll`:

```
csc /target:library ConceptAttributes.cs
```

This file _must_ be referenced by anything using concepts.

Then, you can compile the examples:

* The examples in the `concepts\tests` directory have a `Makefile` that can
  be used with `nmake`.  These reference the compiler built above, so remember
  to build `csc`!
* Other examples have `MSBuild` solutions: you will need to open these with a
  version of Visual Studio with our `csc` added (ie, run from the
  `CompilerExtensions` project of Roslyn).  See above.
* To build individual files using concepts, just use our `csc`.
  _Remember to reference (`/reference`) `ConceptAttributes.dll`_: things break
  without it.
