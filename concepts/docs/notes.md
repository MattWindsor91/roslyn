Notes


# Status update
- FI to current Roslyn
- bug fixes

# Features added:

- dictionary sharing (improves perf for legacy JIT, no change for Ryu JIT)
- instance methods using existing extension syntax (``this``)
- type level properties (eg. ``Num<int>.Zero`` but not ``int.Zero``)
- auto-filling of instance methods and operators for instances (static methods NYI).
- standalone instances without concepts (to model simple extensions, avoid duplication)
- improved type inference driven by Linq
    * allows type information to propagate from concept constraints to ordinary type inference.
      akin to output type inference for untyped delegate parameters.


# Linq update

Question: can we Concepts to speed up Linq to objects

Linq to objects (Current) 
   - Least common denominator implementation over IEnumberable<T>
     
   - Baked in specialization for handfull of types, eg. Select
   - Lots of boxing and virtual calls due to IEnumberator<T>.

Concept Linq:
   - One generic implementation with open specializations
   - Specialization based on properties of types unrelated to subtyping, ie. Random Access vs Sequential 
   - ...

Answer: Yes but...
   - Sum/Average
   - Specialized but open Select
   - Specialized but open SelectMany
   - Benchmarks
   - Limitations: 
     - generic soup: 
     - two reason: lack of higher kinds requires 
     - relational encoding of source/destination collection
                        prevents abstraction over collection types
     - lack of associated types requires extra parameterization

- Examples
  - Practical:
    - Revised Numeric Tower (Haskell Prelude); illustrates autofilling
    - Generic Numeric Algorithms: Quickhull 
    - Beautiful Differentiation (Forward Automatic Differentiation)
    - JSON Serialization and Deserialization
    - Linq
  - Academic:
    - Shapes Comparison
    - (Some/All?) JavaGI examples
    - Siek's et al. Generic Graph Algorithm
    - Property Based Testing (Smallcheck for C#)    
    - Normalization by Evaluation 
    
- Design Issues
    - Explicit vs Implicit dictionary parameters:
       - [+] supports disambiguation
       - [+] explicit simplifies generic override story
       - [+] allows one type parameter for many concepts
       - [+] clear interop story
       - [-] harder to use/less concise
       - [-] positional notation is all or nothing (better: named type arguments for partial instantiation).
    - What is the correct place to insert concept witness inference? Who's the expert?
    - Should we support associated types - everyone else does
        - cuts down on explicit type parameters, simplifies relational encoding of higher-kinded types.
        - can we do it later? Re-use C# proposal by Siek et al.
    - Should we propagate constraints implicitly (a la Siek et al)
    - For static methods, should we insist on qualifier or not (as we do now) 
      or add ``using static <Concept>;``
    - Multiparameter concepts are very useful, eg. Linq. 
      All competitors either support MP concepts or associated Types.
    - Coherence 
    - Should we allow redefinition of built-in operators with different semantics (or not)

- TODO 
    - run existing unit tests
    - new unit tests
