# Concept-C# Code

This directory contains:

- the Concept-C# support attributes, `ConceptAttributes\`, which are needed to
  write concept code;
- the Concept-C# 'standard library', `ConceptLibrary\`;
- examples of Concepts in action.

## Examples

### Feature testbeds

- `AssociatedTypes`: feature testbed for associated types.
- `AutofilledInstances`: feature testbed for auto-filled instances
  (calling existing methods and operators if the instance doesn't
  implement them).
- `ConceptExtensionMethods`: feature testbed for concept extension methods.
- `Defaults`: feature testbed for default instance members.
- `OperatorOverloads`: feature testbed for operator overloading.
- `StandaloneInstances`: feature testbed for instances without concepts.

### Experiments

- `conceptbench`: Benchmarks comparing Concept-C# concepts, hand-coded
  concepts, interfaces, and manually specialised code.
- `CsTypeClasses-PrototypeSyntax`: one of the first example sets we did,
  a port of Claudio Russo's original pitch for typeclasses (`CsTypeClasses`)
- `Enumerable`: small examples for concept-based enumerables and
  enumerators, including building enumerators 'for free' from smaller
  concepts such as 'is indexable' and 'has length'.
- `ExpressionUtils`: work on normalisation by evaluation and concepts
  involving LINQ-style expression trees.
- `Monoids`: small examples for using monoids.
- `ReadmeExample`: a version of the example given in the project readme.
- `ShapesComparison`: side-by-side comparison of implemented Concept-C#
  features against Shapes ones.

### Full examples

- `BeautifulDifferentiation`: implementation of Conal Elliott's functional
  automatic differentiation system in concepts, including lifting numeric
  operations onto functions and expressions.
  to Concept-C#.
- `Quickhull`: an implementation of the Quickhull convex hull algorithm,
  generic (to a degree) on the numeric types used for calculation.  Also
  uses monoid and ordering concepts to perform calculations.
- `RWHSimpleJson`: demonstrating how concepts can be used for type-directed
  serialisation and deserialisation with a port of _Real World Haskell_'s
  toy JSON library.
- `SerialPBT`: a simple property based tester based on Runciman et al.'s
  _SmallCheck_ for Haskell.
- `TCOIExamples`: porting of some of the examples from the
  _Type Classes as Objects and Implicits_ paper, eg. comparing Concept-C#
  to Scala.
- `TinyLinq`: implementation of part of the _LINQ to Objects_ query pattern
  using concepts, demonstrating type-directed specialisation.
- `TupleConcepts`: using concepts to abstract over tuple-like types,
  including `ValueTuple` and `Tuple`.