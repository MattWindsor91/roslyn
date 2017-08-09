# Concepts

This directory contains experimental work on implementing concepts for C#, by
Claudio Russo and Matt Windsor.

## Concept attributes DLL

To use concepts, you must compile and reference `ConceptAttributes.cs` in this
directory as an assembly, and reference it in any concept-using code:

```
csc /target:library /out:ConceptAttributes.DLL ConceptAttributes.cs
```

Any `csc` will do.

## Test cases

There are some test cases in `code/` and `tests/`.  These can be compiled with
the `csc` from this repository.  Remember to add `ConceptAttributes` as a
reference:

```
csc /reference:..\ConceptAttributes.DLL <name>.cs
```