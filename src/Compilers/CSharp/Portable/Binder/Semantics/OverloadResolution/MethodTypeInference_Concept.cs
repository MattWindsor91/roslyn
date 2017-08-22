using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class MethodTypeInferrer
    {
        /// <summary>
        /// Performs the concept phase of type inference.
        /// <para>
        /// This phase occurs when the vanilla C# first and second phases have
        /// both failed.
        /// </para>
        /// <para>
        /// In this phase, we check to see whether the remaining unbound
        /// type parameters are concept witnesses.  If they are, then we
        /// find all currently visible implementations of the witnessed
        /// concept in scope, and check whether the set of implementations
        /// yields a viable type for the missing argument.
        /// </para>
        /// </summary>
        /// <param name="binder">
        /// The binder for the scope in which the type-inferred method
        /// resides.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// The diagnostics set for this use site.
        /// </param>
        /// <returns></returns>
        private bool InferTypeArgsConceptPhase(Binder binder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(!AllFixed(),
                "Concept witness inference is pointless if there is nothing to infer");

            var inferrer = ConceptWitnessInferrer.ForBinder(binder);
            var fixedMap = inferrer.Infer(_methodTypeParameters, _fixedResults.AsImmutable(), false, new ImmutableTypeMap(), ImmutableHashSet<NamedTypeSymbol>.Empty);
            if (fixedMap == null)
            {
                return false;
            }

            for (int i = 0; i < _fixedResults.Length; i++)
            {
                if (_fixedResults[i] != null)
                {
                    // This type parameter was already fixed.
                    continue;
                }

                /* Note: One might be tempted to put in an assertion here that
                   the map can't assign to _fixedResults[i] the same thing as
                   _methodTypeParameters[i].  However, sometimes it can!

                   For example, consider

                   private static void Qsort<T, implicit OrdT>
                       (T[] xs, int lo, int hi) where OrdT : Ord<T>
                   {
                       if (lo < hi)
                       {
                           var p = Partition(xs, lo, hi);
                           Qsort(xs, lo, p - 1);
                           Qsort(xs, p + 1, hi);
                       }
                  }

                  In this case, the map will resolve the missing
                  type parameters in each Qsort call as T->T, OrdT->OrdT.
                  These are exactly the same symbols. */
                _fixedResults[i] = fixedMap.SubstituteType(_methodTypeParameters[i]).AsTypeSymbolOnly();
            }

            return true;
        }
    }

    /// <summary>
    /// An object that, given a series of viable instances and bound type
    /// parameters, can perform concept witness inference.
    /// </summary>
    internal class ConceptWitnessInferrer
    {
        /// <summary>
        /// The list of all instances in scope for this inferrer.
        /// These can be either type parameters (eg. witnesses passed in
        /// through constraints at the method or class level) or named
        /// types (instance declarations).
        /// </summary>
        private readonly ImmutableArray<TypeSymbol> _allInstances;

        /// <summary>
        /// The set of all type parameters in scope that are bound:
        /// we cannot substitute for them in unification.  Usually this is
        /// the set of type parameters introduced through type parameter
        /// lists on methods and classes in scope.
        /// </summary>
        private readonly ImmutableHashSet<TypeParameterSymbol> _boundParams;

        /// <summary>
        /// A candidate instance and its unification.
        /// </summary>
        internal struct Candidate
        {
            /// <summary>
            /// The candidate instance.
            /// </summary>
            public readonly TypeSymbol Instance;

            /// <summary>
            /// The unification that must be made to accept this instance.
            /// </summary>
            public readonly ImmutableTypeMap Unification;

            /// <summary>
            /// Constructs a Candidate.
            /// </summary>
            /// <param name="instance">
            /// The candidate instance.
            /// </param>
            /// <param name="unification">
            /// The unification that must be made to accept this instance.
            /// </param>
            public Candidate(TypeSymbol instance, ImmutableTypeMap unification)
            {
                Instance = instance;
                Unification = unification;
            }
        }

        /// <summary>
        /// Constructs a new ConceptWitnessInferrer.
        /// </summary>
        /// <param name="allInstances">
        /// The list of all instances in scope for this inferrer.
        /// </param>
        /// <param name="boundParams">
        /// The set of all type parameters in scope that are bound, and
        /// cannot be substituted out in unification.
        /// </param>
        public ConceptWitnessInferrer(ImmutableArray<TypeSymbol> allInstances, ImmutableHashSet<TypeParameterSymbol> boundParams)
        {
            _allInstances = allInstances;
            _boundParams = boundParams;
        }

        #region Setup from binder

        /// <summary>
        /// Constructs a new ConceptWitnessInferrer taking its instance pool
        /// and bound parameter set from a given binder.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for the new inferrer.
        /// </param>
        /// <returns>
        /// An inferrer that will consider all instances in scope at the given
        /// binder, and refuse to unify on any type parameters bound in methods
        /// or classes in the binder's vicinity.
        /// </returns>
        public static ConceptWitnessInferrer ForBinder(Binder binder)
        {
            // We need two things from the outer scope:
            // 1) All instances visible to this method call;
            // 2) All type parameters bound in the method and class.
            // For efficiency, we do these in one go.
            // TODO: Ideally this should be cached at some point, perhaps on the
            // compilation or binder.
            (var allInstances, var boundParams) = SearchScopeForInstancesAndParams(binder);
            return new ConceptWitnessInferrer(allInstances, boundParams);
        }

        /// <summary>
        /// Traverses the scope induced by the given binder for visible
        /// instances and fixed type parameters.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <returns>
        /// An immutable array of symbols (either type parameters or named
        /// types) representing concept instances available in the scope
        /// of <paramref name="binder"/>, and the set of type parameters
        /// that are already fixed in <paramref name="binder"/>.
        /// </returns>
        private static (ImmutableArray<TypeSymbol>, ImmutableHashSet<TypeParameterSymbol>) SearchScopeForInstancesAndParams(Binder binder)
        {
            var iBuilder = new ArrayBuilder<TypeSymbol>();
            var fpBuilder = new ArrayBuilder<TypeParameterSymbol>();

            var ignore = new HashSet<DiagnosticInfo>();

            for (var b = binder; b != null; b = b.Next)
            {
                b.GetConceptInstances(false, iBuilder, binder, ref ignore);
                b.GetFixedTypeParameters(fpBuilder);
            }

            iBuilder.RemoveDuplicates();
            var allInstances = iBuilder.ToImmutableAndFree();
            var fixedParams = fpBuilder.ToImmutableHashSet();
            fpBuilder.Free();

            return (allInstances, fixedParams);
        }

        /// <summary>
        /// Adds all constraint witnesses in a parent member or type to an array.
        /// </summary>
        /// <param name="container">
        /// The container symbol to query.
        /// </param>
        /// <param name="instances">
        /// The instance array to populate with witnesses.
        /// </param>
        /// <param name="fixedParams">
        /// The set to populate with fixed type parameters.
        /// </param>
        private static void SearchContainerForInstancesAndParams(Symbol container,
            ref ArrayBuilder<TypeSymbol> instances,
            ref HashSet<TypeParameterSymbol> fixedParams)
        {
            // Only methods and named types have constrained witnesses.
            if (container.Kind != SymbolKind.Method && container.Kind != SymbolKind.NamedType) return;

            ImmutableArray<TypeParameterSymbol> tps = GetTypeParametersOf(container);

            foreach (var tp in tps)
            {
                if (tp.IsConceptWitness) instances.Add(tp);
                fixedParams.Add(tp);
            }
        }

        /// <summary>
        /// Adds all named-type instances inside a container and visible in this scope to an array.
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for this query.
        /// </param>
        /// <param name="container">
        /// The current container being searched for instanes.
        /// </param>
        /// <param name="instances">
        /// The instance array to populate with witnesses.
        /// </param>
        private static void GetNamedInstances(Binder binder, Symbol container, ref ArrayBuilder<TypeSymbol> instances)
        {
            var ignore = new HashSet<DiagnosticInfo>();

            // Only namespaces and named kinds can have named instances.
            if (container.Kind != SymbolKind.Namespace && container.Kind != SymbolKind.NamedType) return;

            foreach (var member in ((NamespaceOrTypeSymbol)container).GetTypeMembers())
            {
                if (!binder.IsAccessible(member, ref ignore, binder.ContainingType)) continue;

                // Assuming that instances don't contain sub-instances.
                if (member.IsInstance) instances.Add(member);
            }
        }

        /// <summary>
        /// Filters a set of type parameters into a fixed map, unfixed concept
        /// witnesses and unfixed associated types, and checks that there are
        /// no other unfixed parameters. 
        /// </summary>
        /// <param name="typeParameters">
        /// The set of type parameters being inferred.
        /// </param>
        /// <param name="typeArguments">
        /// The set of already-inferred type arguments; unfixed parameters must
        /// either be represented by a null, or a copy of the corresponding
        /// type parameter.
        /// </param>
        /// <returns>
        /// A tuple of:
        /// <list type="bullet">
        /// <item>
        /// A Boolean true if, and only if, each unfixed type parameter is a
        /// concept witness or associated type;
        /// </item>
        /// <item>
        /// An array of unfixed concept witness indices;
        /// </item>
        /// <item>
        /// An array of unfixed associated type parameters;
        /// </item>
        /// <item>
        /// A map from fixed type parameters to type arguments.
        /// </item>
        /// </list>
        /// </returns>
        internal (bool success, ImmutableArray<int> conceptIndices, ImmutableArray<int> associatedIndices, ImmutableTypeMap fixedParamMap) PartitionTypeParameters(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeSymbol> typeArguments
        )
        {
            Debug.Assert(typeParameters.Length == typeArguments.Length,
                "There should be as many type parameters as arguments.");

            var wBuilder = ArrayBuilder<int>.GetInstance();
            var aBuilder = ArrayBuilder<int>.GetInstance();
            var fixedMapB = new MutableTypeMap();

            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (TypeArgumentIsFixed(typeArguments[i]))
                {
                    fixedMapB.Add(typeParameters[i], new TypeWithModifiers(typeArguments[i]));
                    continue;
                }
                // If we got here, the parameter is unfixed.

                if (typeParameters[i].IsConceptWitness) wBuilder.Add(i);
                else if (typeParameters[i].IsAssociatedType) aBuilder.Add(i);
                else
                {
                    // If we got here, the type parameter is unfixed, but is
                    // neither a concept witness nor an associated type.  Our
                    // inferrer can't possibly fix these, so we give up. 
                    wBuilder.Free();
                    aBuilder.Free();
                    return (false, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty, new ImmutableTypeMap());
                }
            }

            return (true, wBuilder.ToImmutableAndFree(), aBuilder.ToImmutableAndFree(), fixedMapB.ToUnification());
        }

        /// <summary>
        /// Decides whether a given type argument is fixed (successfully
        /// inferred).
        /// </summary>
        /// <param name="typeArgument">
        /// The type argument to check.
        /// </param>
        /// <returns>
        /// True if the argument is fixed.  This method may sometimes
        /// return false negatives, which affects completeness
        /// (some valid type inference may fail) but not soundness.
        /// </returns>
        private bool TypeArgumentIsFixed(TypeSymbol typeArgument)
        {
            // @t-mawind
            //   This is slightly ad-hoc and needs checking.
            //   The intuition is that:
            //   1) In some places (eg. method inference), unfixed type
            //      arguments are always null, so we can just check for null.
            if (typeArgument == null)
            {
                return false;
            }
            //   2) In other places, they are some type parameter.
            if (typeArgument.Kind != SymbolKind.TypeParameter)
            {
                return true;
            }
            //      We assume that, once the type argument becomes something
            //      other than a type parameter, it's been fixed.  However,
            //      that parameter might _not_ be the same as the corresponding
            //      type parameter of the argument, because it may have been
            //      unified with another unfixed type argument!  (This happens
            //      when we're in the middle of associated type inference).
            //
            //      For now, we just assume that any type parameter that is not
            //      one of the 'bound' parameters (ie universally quantified
            //      instead of existential) is evidence of being unfixed.
            //      This is probably wrong.
            return _boundParams.Contains(typeArgument as TypeParameterSymbol);
        }

        #endregion Setup from binder
        #region Main driver

        internal ImmutableTypeMap Infer(ImmutableArray<TypeParameterSymbol> typeParameters, ImmutableArray<TypeSymbol> typeArguments, bool treatUnboundAsUnfixed, ImmutableTypeMap existingFixedMap, ImmutableHashSet<NamedTypeSymbol> chain)
        {
            Debug.Assert(typeParameters.Length == typeArguments.Length,
                "should have as many type parameters as type arguments");

            (bool success, var conceptIndices, var associatedIndices, var fixedMap) =
                PartitionTypeParameters(typeParameters, typeArguments);
            if (!success)
            {
                // This instance has some unfixed non-witness/non-associated type
                // parameters.  We can't infer these, so give up on this
                // candidate instance.
                return default;
            }

            return InferWithClassifiedParameters(conceptIndices, associatedIndices, typeParameters, existingFixedMap.Compose(fixedMap), chain);
        }

        /// <summary>
        /// Tries to infer a batch of concept witnesses given a fully
        /// classified set of type parameters and a map of previous type
        /// parameter fixings.
        /// </summary>
        /// <param name="conceptIndices">
        /// An array containing the indices into
        /// <paramref name="allTypeParameters"/> that have been marked as
        /// witnesses to infer.
        /// </param>
        /// <param name="associatedIndices">
        /// The array of indices of unfixed associated types to infer.
        /// </param>
        /// <param name="allTypeParameters">
        /// The entire set of type parameters in this inference round,
        /// indexed by <paramref name="associatedIndices"/>
        /// </param>
        /// <param name="existingFixedMap">
        /// A fixed map containing any previous unifications made during
        /// concept inference, as well as any fixed type
        /// parameters on the parent instance, method or class of this
        /// inference run.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// The final unification map if inference succeeded; the default map otherwise.
        /// </returns>
        internal ImmutableTypeMap InferWithClassifiedParameters(
            ImmutableArray<int> conceptIndices,
            ImmutableArray<int> associatedIndices,
            ImmutableArray<TypeParameterSymbol> allTypeParameters,
            ImmutableTypeMap existingFixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain)
        {
            // Early out: if we don't have any concept indices, we can't infer.
            if (conceptIndices.IsEmpty)
            {
                return existingFixedMap;
            }

            bool inferredAll;

            // Our goal is to infer both associated types and concept witnesses
            // here.  We first do the latter, then the former.
            //
            // This is because associated types are generally not known until
            // we happen to fix a concept witness that names the associated
            // type in one of its type parameters, ie the witness that
            // 'defines' the associated type.
            //
            // Although concept witnesses may themselves depend on associated
            // types, any substitutions we make that could infer them come
            // precisely from the concept witnesses.  This means we can just
            // keep iterating over the concept witnesses and any insights from
            // the associated types will be applied automatically.
            //
            // With this in mind, what we do is:
            //
            // 1) Substitute in the set of substitutions made so far in the
            //    inference path leading to this call.  This includes any
            //    already-inferred type parameters as well as any unifications
            //    made during concept witness inference earlier in the chain.
            // 2) Try to fix all concept witnesses using the substitution from
            //    1), appending to it any unifications made in any recursive
            //    call inside the concept witness inference round.
            // 3) If we made no progress in 2) and there are some unfixed types
            //    left, fail.  Otherwise, if no unfixed types remain, succeed.
            //    Otherwise, return to 1) with the substitution from 2).
            var currentSubstitution = existingFixedMap;
            do
            {
                bool conceptProgress = false;
                if (!conceptIndices.IsEmpty)
                {
                    var newConceptIndices = TryInferConceptWitnesses(conceptIndices, allTypeParameters, existingFixedMap, chain, ref currentSubstitution);
                    conceptProgress = conceptProgress || (newConceptIndices.Length < conceptIndices.Length);
                    conceptIndices = newConceptIndices;
                }

                inferredAll = conceptIndices.IsEmpty;

                // Stop if we made no progress whatsoever.
                if (!conceptProgress && !inferredAll)
                {
                    return default;
                }
            } while (!inferredAll);

            // Once we've inferred all concept witnesses, all associated types
            // will either be fixed or not inferrable.  The parent can check
            // for this.
            return currentSubstitution;
        }

        /// <summary>
        /// Tries to infer a batch of concept witnesses.
        /// </summary>
        /// <param name="conceptIndices">
        /// An array containing the indices into
        /// <paramref name="allTypeParameters"/> that have been marked as
        /// witnesses to infer.
        /// </param>
        /// <param name="allTypeParameters">
        /// The entire set of type parameters in this inference round,
        /// indexed by <paramref name="conceptIndices"/>
        /// </param>
        /// <param name="parentSubstitution">
        /// A substitution applying all of the unifications made in previous
        /// inferences in a recursive chain, as well as any fixed type
        /// parameters on the parent instance, method or class of this
        /// inference run.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <param name="currentSubstitution">
        /// The current set of substitutions that have been made in this round
        /// of inference, to which this method will add any unifications made
        /// when fixing the current concept witnesses.  This is then used to
        /// fix associated type parameters.
        /// </param>
        /// <returns>
        /// An array of fixed concept witnesses.  If any are null, then
        /// inference has not succeeded.
        /// </returns>
        private ImmutableArray<int> TryInferConceptWitnesses(
            ImmutableArray<int> conceptIndices,
            ImmutableArray<TypeParameterSymbol> allTypeParameters,
            ImmutableTypeMap parentSubstitution,
            ImmutableHashSet<NamedTypeSymbol> chain,
            ref ImmutableTypeMap currentSubstitution)
        {
            var resultsBuilder = ArrayBuilder<Candidate>.GetInstance();

            foreach (int i in conceptIndices)
            {
                resultsBuilder.Add(InferOneWitness(allTypeParameters[i], parentSubstitution, chain));
            }
            var maybeFixed = resultsBuilder.ToImmutableAndFree();

            var newConceptIndices = ArrayBuilder<int>.GetInstance();
            for (int i = 0; i < maybeFixed.Length; i++)
            {
                if (maybeFixed[i].Instance == null)
                {
                    // Inference failed, so put this index back onto the list
                    // for the next cycle.
                    newConceptIndices.Add(conceptIndices[i]);
                    continue;
                };

                Debug.Assert(maybeFixed[i].Instance.IsInstanceType() || maybeFixed[i].Instance.IsConceptWitness,
                    "Concept witness inference returned something other than a concept instance or witness");

                // TODO: not a graceful way to handle errors from Merge...
                currentSubstitution = currentSubstitution?.Compose(maybeFixed[i].Unification);
                if (currentSubstitution == null) return conceptIndices;

                // Finally, add the inferred concept into the substitution.
                currentSubstitution = currentSubstitution.Add(allTypeParameters[conceptIndices[i]], new TypeWithModifiers(maybeFixed[i].Instance));
            }
            return newConceptIndices.ToImmutableAndFree();
        }

        /// <summary>
        /// Tries to infer a suitable instance for the given concept witness
        /// type parameter.
        /// </summary>
        /// <param name="typeParam">
        /// The type parameter that is the concept witness to infer.  This
        /// must actually be a concept witness.
        /// </param>
        /// <param name="fixedMap">
        /// The map from all of the fixed, non-witness type parameters in the
        /// same type parameter list as <paramref name="typeParam"/>
        /// to their arguments.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// Null if inference failed; else, the inferred concept instance and
        /// its unification.
        /// </returns>
        internal Candidate InferOneWitness(TypeParameterSymbol typeParam, ImmutableTypeMap fixedMap, ImmutableHashSet<NamedTypeSymbol> chain = null)
        {
            Debug.Assert(typeParam.IsConceptWitness,
                "Tried to do concept witness inference on a non-concept-witness type parameter");

            var requiredConcepts = GetRequiredConceptsFor(typeParam, fixedMap);
            return InferOneWitnessFromRequiredConcepts(requiredConcepts, fixedMap, chain);
        }

        /// <summary>
        /// Tries to infer a suitable instance for the given set of required
        /// concepts.
        /// <para>
        /// This is useful when we are trying to find a suitable witness in a
        /// situation where there is no actual type parameter to be inferred.
        /// </para>
        /// </summary>
        /// <param name="requiredConcepts">
        /// The list of concepts the candidate must implement.
        /// </param>
        /// <param name="fixedMap">
        /// The map from all of the fixed, non-witness type parameters in the
        /// current context to their arguments.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// Null if inference failed; else, the inferred concept instance and
        /// its unification.
        /// </returns>
        internal Candidate InferOneWitnessFromRequiredConcepts(ImmutableArray<TypeSymbol> requiredConcepts, ImmutableTypeMap fixedMap, ImmutableHashSet<NamedTypeSymbol> chain = null)
        {
            // Sometimes, required concepts will be empty.  This is usually when
            // a type parameter being inferred is erroneous, as we try to forbid
            // parameters with no required concepts at constraint checking level.
            // These are useless and we don't infer them.
            if (requiredConcepts.IsDefaultOrEmpty)
            {
                return default;
            }

            // From here, we can only decrease the number of considered
            // instances, so we can't assign an instance to a witness
            // parameter if there aren't any to begin with.
            if (_allInstances.IsDefaultOrEmpty)
            {
                return default;
            }

            if (chain == null)
            {
                chain = ImmutableHashSet<NamedTypeSymbol>.Empty;
            }

            // @t-mawind
            // An instance satisfies inference if:
            //
            // 1) for all concepts required by the type parameter, at least
            //    one concept implemented by the instances unifies with that
            //    concept without capturing bound type parameters;
            // 2) all of the type parameters of that instance can be bound,
            //    both by the substitutions from the unification above and also
            //    by recursively trying to infer any missing concept witnesses.
            //
            // The first part is equivalent to establishing
            //    witness :- instance.
            //
            // The second part is equivalent to resolving
            //    instance :- dependency1; dependency2; ...
            // by trying to establish the dependencies as separate queries.
            //
            // After the second part, if we have multiple possible instances,
            // we try to see if one implements a subconcept of all of the other
            // instances.  If so, we narrow to that specific instance.
            //
            // If we have multiple satisfying instances, or zero, we fail.

            var firstPassInstances = AllInstancesSatisfyingGoal(requiredConcepts);
            // We can't infer if none of the instances implement our concept!
            // However, if we have more than one candidate instance at this
            // point, we shouldn't bail until we've made sure only one of them
            // passes 2).
            if (firstPassInstances.IsDefaultOrEmpty)
            {
                return default;
            }
            Debug.Assert(firstPassInstances.Length <= _allInstances.Length,
                "First pass of concept witness inference should not grow the instance list");

            var secondPassInstances = ToSatisfiableInstances(firstPassInstances, chain);
            if (secondPassInstances.IsDefaultOrEmpty)
            {
                return default;
            }
            Debug.Assert(secondPassInstances.Length <= firstPassInstances.Length,
                "Second pass of concept witness inference should not grow the instance list");

            // We only do tie breaking in the case of actual ties.
            var thirdPassInstances = secondPassInstances;
            if (1 < secondPassInstances.Length) thirdPassInstances = TieBreakInstances(secondPassInstances);
            Debug.Assert(thirdPassInstances.Length <= secondPassInstances.Length,
                "Third pass of concept witness inference should not grow the instance list");

            // Either ambiguity, or an outright lack of inference success.
            if (thirdPassInstances.IsDefaultOrEmpty || thirdPassInstances.Length != 1)
            {
                return default;
            }
            Debug.Assert(thirdPassInstances[0].Instance != null,
                "Inference claims to have succeeded, but has returned a null instance");
            return thirdPassInstances[0];
        }

        /// <summary>
        /// Deduces the set of concepts that must be implemented by any witness
        /// supplied to the given type parameter.
        /// </summary>
        /// <param name="typeParam">
        /// The type parameter being inferred.
        /// </param>
        /// <param name="fixedMap">
        /// A map mapping fixed type parameters to their type arguments.
        /// </param>
        /// <returns>
        /// An array of concepts required by <paramref name="typeParam"/>.
        /// </returns>
        private static ImmutableArray<TypeSymbol> GetRequiredConceptsFor(TypeParameterSymbol typeParam, ImmutableTypeMap fixedMap)
        {
            //TODO: error if interface constraint that is not a concept?
            var rawRequiredConcepts = typeParam.AllEffectiveInterfacesNoUseSiteDiagnostics;

            // The concepts from above are in terms of the method's type
            // parameters.  In order to be able to unify properly, we need to
            // substitute the inferences we've made so far.
            var rc = new ArrayBuilder<TypeSymbol>();
            foreach (var con in rawRequiredConcepts)
            {
                rc.Add(fixedMap.SubstituteType(con).AsTypeSymbolOnly());
            }

            var unused = new HashSet<DiagnosticInfo>();

            // Now we can do some optimisation: if we're asking for a concept,
            // we don't need to ask for its base concepts.
            // This is analogous to Haskell context reduction, but somewhat
            // simpler: because of the way our concepts are architected, much
            // of what Haskell does makes no sense.
            var rc2 = new ArrayBuilder<TypeSymbol>();
            foreach (var c1 in rc)
            {
                var needed = true;
                foreach (var c2 in rc)
                {
                    if (c2.ImplementsInterface(c1, ref unused))
                    {
                        needed = false;
                        break;
                    }
                }
                if (needed) rc2.Add(c1);
            }
            rc.Free();
            return rc2.ToImmutableAndFree();
        }

        /// <summary>
        /// Gets the type parameters of an arbitrary symbol.
        /// </summary>
        /// <param name="symbol">
        /// The symbol for which we are getting type parameters.
        /// </param>
        /// <returns>
        /// If the symbol is a generic method or named type, its parameters;
        /// else, the empty list.
        /// </returns>
        internal static ImmutableArray<TypeParameterSymbol> GetTypeParametersOf(Symbol symbol)
        {
            switch (symbol)
            {
                case MethodSymbol m:
                return m.TypeParameters;
                case NamedTypeSymbol m:
                return m.TypeParameters;
                default:
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        #endregion Main driver
        #region First pass

        /// <summary>
        /// Performs the first pass of concept witness type inference.
        /// <para>
        /// This pass filters down a list of all possible instances into a set
        /// of candidate instances, such that each candidate instance
        /// implements all of the concepts required by the parameter being
        /// inferred.
        /// </para>
        /// </summary>
        /// <param name="requiredConcepts">
        /// The list of concepts required by the type parameter being inferred.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the first pass.
        /// </returns>
        private ImmutableArray<Candidate> AllInstancesSatisfyingGoal(ImmutableArray<TypeSymbol> requiredConcepts)
        {
            Debug.Assert(!requiredConcepts.IsEmpty,
                "First pass of inference is pointless when there are no required concepts");
            Debug.Assert(!_allInstances.IsEmpty,
                "First pass of inference is pointless when there are no available instances");

            // First, collect all of the instances satisfying 1).
            var firstPassInstanceBuilder = new ArrayBuilder<Candidate>();
            foreach (var instance in _allInstances)
            {
                if (AllRequiredConceptsProvided(requiredConcepts, instance, out ImmutableTypeMap unifyingSubstitutions))
                {
                    // The unification may have provided us with substitutions
                    // that were needed to make the provided concepts fit the
                    // required concepts.
                    //
                    // It may be that some of these substitutions also need to
                    // apply to the actual instance so it can satisfy #2.
                    var result = unifyingSubstitutions.SubstituteType(instance).AsTypeSymbolOnly();
                    firstPassInstanceBuilder.Add(new Candidate(result, unifyingSubstitutions));
                }
            }
            return firstPassInstanceBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Checks whether a list of required concepts is implemented by a
        /// candidate instance modulo unifying substitutions.
        /// <para>
        /// We don't check yet that the instance itself is satisfiable, just that
        /// it will satisfy our concept list if it is.
        /// </para>
        /// </summary>
        /// <param name="requiredConcepts">
        /// The list of required concepts to implement.  Must be non-empty.
        /// </param>
        /// <param name="instance">
        /// The candidate instance.
        /// </param>
        /// <param name="unifyingSubstitutions">
        /// A map of type substitutions, populated by this method, which are
        /// required in order to make the instance implement the concepts.
        /// </param>
        /// <returns>
        /// True if, and only if, the given instance implements the given list
        /// of concepts.
        /// </returns>
        private bool AllRequiredConceptsProvided(ImmutableArray<TypeSymbol> requiredConcepts, TypeSymbol instance, out ImmutableTypeMap unifyingSubstitutions)
        {
            Debug.Assert(!requiredConcepts.IsEmpty,
                "Checking that all required concepts are provided is pointless when there are none");

            var subst = new MutableTypeMap();
            unifyingSubstitutions = new ImmutableTypeMap();

            var providedConcepts =
                ((instance as TypeParameterSymbol)?.AllEffectiveInterfacesNoUseSiteDiagnostics
                 ?? ((instance as NamedTypeSymbol)?.AllInterfacesNoUseSiteDiagnostics)
                 ?? ImmutableArray<NamedTypeSymbol>.Empty);
            if (providedConcepts.IsEmpty) return false;

            foreach (var requiredConcept in requiredConcepts)
            {
                if (!IsRequiredConceptProvided(requiredConcept, providedConcepts, ref subst)) return false;
            }

            // If we got here, all required concepts must have been provided.
            unifyingSubstitutions = subst.ToUnification();
            return true;
        }

        /// <summary>
        /// Checks whether a single required concept is implemented by a
        /// set of provided concepts modulo unifying substitutions.
        /// <para>
        /// We don't check yet that the instance itself is satisfiable, just that
        /// it will satisfy our concept list if it is.
        /// </para>
        /// </summary>
        /// <param name="requiredConcept">
        /// The required concept to implement.
        /// </param>
        /// <param name="providedConcepts">
        /// The provided concepts to check against.  Must be non-empty.
        /// </param>
        /// <param name="unifyingSubstitutions">
        /// A map of type substitutions, added to by this method, which are
        /// required in order to make the instance implement the concepts.
        /// Any existing substitutions in this map, for example those fixed
        /// by previous required concepts, are applied during unification.
        /// </param>
        /// <returns>
        /// True if, and only if, the given set of provided concepts implement
        /// the given list of concepts.
        /// </returns>
        private bool IsRequiredConceptProvided(TypeSymbol requiredConcept, ImmutableArray<NamedTypeSymbol> providedConcepts, ref MutableTypeMap unifyingSubstitutions)
        {
            Debug.Assert(!providedConcepts.IsEmpty,
                "Checking for provision of concept is pointless when no concepts are provided");

            foreach (var providedConcept in providedConcepts)
            {
                if (TypeUnification.CanUnify(providedConcept, requiredConcept, ref unifyingSubstitutions, _boundParams)) return true;
            }

            return false;
        }

        #endregion First pass
        #region Second pass

        /// <summary>
        /// Performs the second pass of concept witness type inference.
        /// <para>
        /// This pass tries to fix any witness parameters in each candidate
        /// instance, eliminating it if it either has unfixed non-witness
        /// parameters, or the witness parameters cannot be fixed.  To do this,
        /// it recursively begins inference on the missing witnesses.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of candidate instances after the first pass.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the first pass.
        /// </returns>
        private ImmutableArray<Candidate> ToSatisfiableInstances(ImmutableArray<Candidate> candidateInstances, ImmutableHashSet<NamedTypeSymbol> chain)
        {
            // Remember: even if we have one instance left here, it could be
            // unsatisfiable, so we have to run this pass on it.
            Debug.Assert(!candidateInstances.IsEmpty,
                "Performing second pass of witness inference is pointless when we have no candidates left");

            var secondPassInstanceBuilder = new ArrayBuilder<Candidate>();
            foreach (var candidate in candidateInstances)
            {
                // Type parameters have no prerequisites to be satisfiable
                // instances.
                if (candidate.Instance.Kind == SymbolKind.TypeParameter)
                {
                    secondPassInstanceBuilder.Add(candidate);
                    continue;
                }

                Debug.Assert(candidate.Instance.Kind == SymbolKind.NamedType,
                    "If an instance is not a parameter, it should be a named type.");

                var nt = (NamedTypeSymbol)candidate.Instance;

                // If we have no type parameters, we can't have any unfixed
                // witnesses.
                if (nt.TypeParameters.IsEmpty)
                {
                    secondPassInstanceBuilder.Add(candidate);
                    continue;
                }

                // @t-mawi TODO: generalise constraint solution?

                // Do cycle detection: have we already set up a recursive
                // call for this instance with these type parameters?
                if (chain.Contains(nt))
                {
                    return default;
                }

                // Assumption: no witness parameter can depend on any other
                // witness parameter, so we can do recursive inference in
                // one pass.

                Candidate fixedInstance = InferRecursively(nt, true, candidate.Unification, chain.Add(nt));
                if (fixedInstance.Instance == null)
                {
                    continue;
                }
                secondPassInstanceBuilder.Add(fixedInstance);
            }
            return secondPassInstanceBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Prepares a recursive inference round on an instance.
        /// </summary>
        /// <param name="unfixedInstance">
        /// The candidate instance, which must be a named type with unfixed type parameters.
        /// </param>
        /// <param name="existingFixedMap">
        /// The map of type substitutions that was used when instantiating the instance.
        /// </param>
        /// <param name="chain">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort the recursive call if it will create a cycle.
        /// </param>
        /// <returns>
        /// Null if recursive inference failed; else, the fully instantiated
        /// candidate, including the extended map with concept and associated type
        /// substitutions.
        /// </returns>
        internal Candidate InferRecursively(
            NamedTypeSymbol unfixedInstance,
            bool treatUnboundAsUnfixed,
            ImmutableTypeMap existingFixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain)
        {
            var unification = Infer(unfixedInstance.TypeParameters, unfixedInstance.TypeArguments, treatUnboundAsUnfixed, existingFixedMap, chain);
            if (unification == null)
            {
                return default;
            }

            var fixedInstance = unification.SubstituteType(unfixedInstance).AsTypeSymbolOnly();
            return new Candidate(fixedInstance, unification);
        }

        #endregion Second pass
        #region Third pass

        /// <summary>
        /// Performs the third pass of concept witness type inference.
        /// <para>
        /// This pass tries to find a single instance in the candidate set that
        /// is 'better' than all other instances, eg. its concept is a strict
        /// super-interface of all other candidate instances' concepts.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to narrow.
        /// </param>
        /// <returns>
        /// An array of candidate instances after the third pass.
        /// </returns>
        private static ImmutableArray<Candidate> TieBreakInstances(ImmutableArray<Candidate> candidateInstances)
        {
            // TODO: better tie-breaking.
            // TODO: formally specify this--it is quite ad-hoc at the moment.

            Debug.Assert(1 < candidateInstances.Length,
                "Tie-breaking is pointless if we have zero or one instances");

            // We now perform an array of 'better concept witness' checks to
            // try to narrow the list of instances to zero or one.

            var mostSpecificConceptInstances = FilterToMostSpecificConceptInstances(candidateInstances);
            Debug.Assert(mostSpecificConceptInstances.Length <= candidateInstances.Length,
                "Filtering to most-specific-concept instances should not grow the instance list");
            if (mostSpecificConceptInstances.Length <= 1) return mostSpecificConceptInstances;

            var mostSpecificParamInstances = FilterToMostSpecificParamInstances(mostSpecificConceptInstances);
            Debug.Assert(mostSpecificParamInstances.Length <= mostSpecificConceptInstances.Length,
                "Filtering to most-specific-param instances should not grow the instance list");

            return mostSpecificParamInstances;
        }

        /// <summary>
        /// Filters a set of candidate instances to those that implement at
        /// least all of the concepts of every other candidate instance.
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to filter.
        /// </param>
        /// <returns>
        /// <paramref name="candidateInstances"/>, filtered to contain only
        /// those instances that implement every concept of every other
        /// instance in <paramref name="candidateInstances"/>.
        /// </returns>
        private static ImmutableArray<Candidate> FilterToMostSpecificConceptInstances(ImmutableArray<Candidate> candidateInstances)
        {
            Debug.Assert(1 < candidateInstances.Length,
                "Filtering to most-specific-concept instances is pointless if we have zero or one instances");

            var arb = new ArrayBuilder<Candidate>();

            // TODO: This can invariably be made more efficient.
            foreach (var instance in candidateInstances)
            {
                if (!ImplementsConceptsOfOtherInstances(instance, candidateInstances)) continue;
                arb.Add(instance);

                // Note that this will only break ties if one instance
                // implements effectively sub-concepts of all other
                // instances: if two instances implement precisely the
                // same concept set, both will be added to arb and the
                // check will fail.
            }

            return arb.ToImmutableAndFree();
        }

        /// <summary>
        /// Checks whether one instance implements all of the concepts, either
        /// directly or through sub-concepts, of a set of other concepts.
        /// </summary>
        /// <param name="instance">
        /// The instance to compare.
        /// </param>
        /// <param name="otherInstances">
        /// A list of other instances to which this instance should be compared.
        /// This may include <paramref name="instance"/>, in which case it will
        /// be ignored.
        /// </param>
        /// <returns>
        /// True if, and only if, <paramref name="instance"/> implements all of
        /// the concepts of instances in <paramref name="otherInstances"/>.
        /// </returns>
        private static bool ImplementsConceptsOfOtherInstances(Candidate instance, ImmutableArray<Candidate> otherInstances)
        {
            Debug.Assert(!otherInstances.IsEmpty,
                "Trying to check whether an instance implements concepts of zero other instances is pointless");

            var ignore = new HashSet<DiagnosticInfo>();

            foreach (var otherInstance in otherInstances)
            {
                if (otherInstance.Instance == instance.Instance) continue;

                foreach (var iface in otherInstance.Instance.AllInterfacesNoUseSiteDiagnostics)
                {
                    if (!iface.IsConcept) continue;
                    if (!instance.Instance.ImplementsInterface(iface, ref ignore)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Filters a set of candidate instances to those whose type parameters
        /// are no less specific than those of any other instance.
        /// <para>
        /// If any instance is more specific than the others, then the others
        /// are less specific and removed, returning the one 'best' instance.
        /// </para>
        /// <para>
        /// Currently, we only rule that one instance is more specific than the
        /// other if it has non-witness type parameters whereas the other does
        /// not.  This is probably overly conservative, however.
        /// </para>
        /// </summary>
        /// <param name="candidateInstances">
        /// The set of instances to filter.
        /// </param>
        /// <returns>
        /// <paramref name="candidateInstances"/>, filtered to contain only
        /// those instances whose type parameters are more specific than those
        /// of any other instance in <paramref name="candidateInstances"/>.
        /// </returns>
        private static ImmutableArray<Candidate> FilterToMostSpecificParamInstances(ImmutableArray<Candidate> candidateInstances)
        {
            Debug.Assert(1 < candidateInstances.Length,
                "Filtering to most-specific-param instances is pointless if we have zero or one instances");

            var arb = new ArrayBuilder<Candidate>();

            // TODO: This can invariably be made more efficient.
            foreach (var instance in candidateInstances)
            {
                if (ParamsLessSpecific(instance, candidateInstances)) continue;
                arb.Add(instance);
            }

            return arb.ToImmutableAndFree();
        }

        /// <summary>
        /// Decides whether an instance is strictly less specific than at least
        /// one other instance.
        /// </summary>
        /// <param name="instance">
        /// The instance to compare.
        /// </param>
        /// <param name="otherInstances">
        /// A list of other instances to which this instance should be compared.
        /// This may include <paramref name="instance"/>, in which case it will
        /// be ignored.
        /// </param>
        /// <returns>
        /// True if, and only if, <paramref name="instance"/> is strictly less
        /// specific than any of the other instances in
        /// <paramref name="otherInstances"/>.
        /// </returns>
        private static bool ParamsLessSpecific(Candidate instance, ImmutableArray<Candidate> otherInstances)
        {
            Debug.Assert(!otherInstances.IsEmpty,
                "Trying to check whether an instance has less specific params than zero other instances is pointless");

            // Currently, we do a very basic check based on non-witness type
            // parameter counts.  This could be much more sophisticated.

            bool instanceHasNonWitnesses = false;
            foreach (var typeParam in GetTypeParametersOf(instance.Instance))
            {
                if (!typeParam.IsConceptWitness)
                {
                    instanceHasNonWitnesses = true;
                    break;
                }
            }

            // No need to do the below check if we don't have non-witness type
            // params: the only way something can be more specific than us at
            // the moment is if we weren't.
            // This will need to go if we do something more sophisticated.
            if (!instanceHasNonWitnesses) return false;

            foreach (var otherInstance in otherInstances)
            {
                if (instance.Instance == otherInstance.Instance) continue;

                // TODO: cache this per instance?
                bool otherHasNonWitnesses = false;
                foreach (var typeParam in GetTypeParametersOf(otherInstance.Instance))
                {
                    if (!typeParam.IsConceptWitness)
                    {
                        otherHasNonWitnesses = true;
                        break;
                    }
                }

                // An instance is more specific if it has no non-witness type
                // parameters, but the other instance does.  Flip this logic to
                // get an early less-specific result.
                if (instanceHasNonWitnesses && !otherHasNonWitnesses) return true;
            }

            return false;
        }

        #endregion Third pass
        #region Part-inference

        /// <summary>
        /// Perform part-inference on the given set of type arguments.
        /// <para>
        /// This is when we are given a set of type arguments for a
        /// generic named type or method with implicit type parameters,
        /// but the type argument list is seemingly missing those parameters.
        /// In this case, we use the concept type inferrer to fill in the
        /// omitted witnesses.
        /// </para>
        /// </summary>
        /// <param name="typeArguments">
        /// The set of present type arguments, which must be lesser than
        /// <paramref name="typeParameters"/> by the number of implicit
        /// type parameters in the latter.
        /// </param>
        /// <param name="typeParameters">
        /// The set of all type parameters, including implicits.
        /// </param>
        /// <param name="expandAssociatedIfFailed">
        /// If true, and there are only associated types missing from
        /// <paramref name="typeArguments"/>, then a failure of
        /// part-inference will return a set of type arguments substituting
        /// the associated type parameters for themselves, instead of an
        /// empty type argument set.  Use for when we are inferring a
        /// concept that is going to be re-inferred for its instance.
        /// </param>
        /// <returns>
        /// The empty array, upon failure; otherwise, a full array of
        /// type arguments that is parallel to <paramref name="typeParameters"/>
        /// and contains the missing arguments.
        /// </returns>
        public ImmutableArray<TypeSymbol> PartInfer(ImmutableArray<TypeSymbol> typeArguments, ImmutableArray<TypeParameterSymbol> typeParameters, bool expandAssociatedIfFailed = false)
        {
            Debug.Assert(typeArguments.Length < typeParameters.Length,
                "Part-inference is pointless if we already have all the type parameters");

            var allArguments = new TypeSymbol[typeParameters.Length];

            // Assume that the missing type arguments are concept witnesses and
            // associated types, and extend the given type arguments with them.
            //
            // To infer the missing arguments, we need a full map from present
            // type parameters to the type arguments we _do_ have.  We can do
            // this at the same time as extending the arguments.
            var conceptIndicesBuilder = ArrayBuilder<int>.GetInstance();
            var associatedIndicesBuilder = ArrayBuilder<int>.GetInstance();
            var fixedMap = new MutableTypeMap();
            int j = 0;
            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (typeParameters[i].IsConceptWitness)
                {
                    conceptIndicesBuilder.Add(i);
                }
                else if (typeParameters[i].IsAssociatedType)
                {
                    associatedIndicesBuilder.Add(i);
                }
                else
                {
                    allArguments[i] = typeArguments[j];
                    fixedMap.Add(typeParameters[i], new TypeWithModifiers(typeArguments[j]));
                    j++;
                }
            }

            var conceptIndices = conceptIndicesBuilder.ToImmutableAndFree();
            var associatedIndices = associatedIndicesBuilder.ToImmutableAndFree();

            var unification = InferWithClassifiedParameters(conceptIndices, associatedIndices, typeParameters, fixedMap.ToUnification(), ImmutableHashSet<NamedTypeSymbol>.Empty);
            if (unification == null)
            {
                // In certain cases, we allow part-inference to return a result
                // if it was only trying to infer associated types, but failed.
                // In this pseudo-result, associated types are left as unfixed
                // type parameters.
                //
                // This is mainly because, in places where we're part-inferring
                // concept with associated types, we might go on to do full
                // inference to replace the concept with one of its instances.
                // In such a case we will, if successful, unify the unfixed
                // associated parameters with something concrete anyway.
                //
                // TODO: ensure that this is sound---there might be places
                // where we claim this is ok, but then don't go on to infer the
                // concept and the result is a spurious type error or crash. 
                if (!expandAssociatedIfFailed || !conceptIndices.IsEmpty)
                {
                    return ImmutableArray<TypeSymbol>.Empty;
            }
            foreach (var i in associatedIndices)
                {
                    allArguments[i] = typeParameters[i];
                }
                return allArguments.ToImmutableArray();
            }

            /* As in normal inference, it is not necessarily a bug for
               the map to map typeParameters[i] to typeParameters[i].
               This might be, eg., a recursive call. */
            foreach (var i in conceptIndices)
            {
                allArguments[i] = unification.SubstituteType(typeParameters[i]).AsTypeSymbolOnly();
            }
            foreach (var i in associatedIndices)
            {
                allArguments[i] = unification.SubstituteType(typeParameters[i]).AsTypeSymbolOnly();
            }

            return allArguments.ToImmutableArray();
        }

        #endregion Part-inference
    }
}
