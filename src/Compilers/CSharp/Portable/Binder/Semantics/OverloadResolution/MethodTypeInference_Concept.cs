using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Text;

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
            var fixedMap = inferrer.Infer(_methodTypeParameters, _fixedResults.AsImmutable(), new ImmutableTypeMap(), ImmutableHashSet<NamedTypeSymbol>.Empty, ref useSiteDiagnostics);
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
        /// The available conversions in scope.
        /// </summary>
        private readonly ConversionsBase _conversions;

        /// <summary>
        /// A candidate instance and its unification.
        /// </summary>
        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        internal struct Candidate
        {
            /// <summary>
            /// Whether the candidate is viable.
            /// </summary>
            public bool Viable;

            /// <summary>
            /// The candidate instance.
            /// </summary>
            public readonly TypeSymbol Instance;

            /// <summary>
            /// The unification that must be made to accept this instance.
            /// </summary>
            public readonly ImmutableTypeMap Unification;

            /// <summary>
            /// The diagnostics, if any, that killed off this candidate.
            /// </summary>
            public HashSet<DiagnosticInfo> Diagnostics;

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
                Diagnostics = null;
                Viable = true;
            }

            /// <summary>
            /// Constructs a failing Candidate with a given diagnostic.
            /// </summary>
            /// <param name="errors">
            /// The set of errors that caused this Candidate to fail.
            /// </param>
            public Candidate(HashSet<DiagnosticInfo> errors)
            {
                Instance = null;
                Unification = null;
                Diagnostics = errors;
                Viable = false;
            }

            private string GetDebuggerDisplay()
            {
                var result = new StringBuilder("[");
                result.Append(GetType().Name);

                result.Append(" (");
                if (Viable)
                {
                    result.Append("viable: ");
                    result.Append(Instance.GetDebuggerDisplay());

                }
                else
                {
                    result.Append("not viable");
                }
                return result.Append(")]").ToString();
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
        /// <param name="conversions">
        /// The conversions in scope at the point where we are doing inference.
        /// </param>
        public ConceptWitnessInferrer(ImmutableArray<TypeSymbol> allInstances, ImmutableHashSet<TypeParameterSymbol> boundParams, ConversionsBase conversions)
        {
            _allInstances = allInstances;
            _boundParams = boundParams;
            _conversions = conversions;
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
            return new ConceptWitnessInferrer(allInstances, boundParams, binder.Conversions);
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
        /// An array of unfixed concept witness parameters;
        /// </item>
        /// <item>
        /// An array of unfixed associated type parameters;
        /// </item>
        /// <item>
        /// A map from fixed type parameters to type arguments.
        /// </item>
        /// </list>
        /// </returns>
        internal (bool success, ImmutableArray<TypeParameterSymbol> conceptParams, ImmutableArray<TypeParameterSymbol> associatedParams, ImmutableTypeMap fixedParamMap) PartitionTypeParameters(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeSymbol> typeArguments
        )
        {
            Debug.Assert(typeParameters.Length == typeArguments.Length,
                "There should be as many type parameters as arguments.");

            var wBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var aBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var fixedMapB = new MutableTypeMap();

            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (TypeArgumentIsFixed(typeArguments[i]))
                {
                    fixedMapB.Add(typeParameters[i], new TypeWithModifiers(typeArguments[i]));
                    continue;
                }
                // If we got here, the parameter is unfixed.

                if (typeParameters[i].IsConceptWitness)
                {
                    wBuilder.Add(typeParameters[i]);
                }
                else if (typeParameters[i].IsAssociatedType)
                {
                    aBuilder.Add(typeParameters[i]);
                }
                else if (typeArguments[i] != null && typeArguments[i].IsAssociatedType)
                {
                    /* @MattWindsor91 (Concept-C# 2017)
                     * We treat a parameter slot as being associated if it has
                     * been assigned to a type argument corresponding to an
                     * unfixed associated type parameter from further up.
                     *
                     * This allows recursive fixing of associated types.
                     */
                    aBuilder.Add((TypeParameterSymbol)typeArguments[i]);
                }
                else
                {
                    // If we got here, the type parameter is unfixed, but is
                    // neither a concept witness nor an associated type.  Our
                    // inferrer can't possibly fix these, so we give up. 
                    wBuilder.Free();
                    aBuilder.Free();
                    return (false, ImmutableArray<TypeParameterSymbol>.Empty, ImmutableArray<TypeParameterSymbol>.Empty, new ImmutableTypeMap());
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
        internal bool TypeArgumentIsFixed(TypeSymbol typeArgument)
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

        /// <summary>
        /// Tries to infer a batch of concept witnesses given parallel
        /// arrays of type arguments and parameters.
        /// </summary>
        /// <param name="typeParams">
        /// The entire set of type parameters in this inference round.
        /// </param>
        /// <param name="typeArguments">
        /// An array, parallel to <paramref name="typeParams"/>,
        /// containing the current fixings for type arguments before inference.
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
        /// <param name="useSiteDiagnostics">
        /// An optional diagnostics bag used to report details on failing inference.
        /// </param>
        /// <returns>
        /// The final unification map if inference succeeded; the default map otherwise.
        /// </returns>
        internal ImmutableTypeMap Infer(
            ImmutableArray<TypeParameterSymbol> typeParams,
            ImmutableArray<TypeSymbol> typeArguments,
            ImmutableTypeMap existingFixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(typeParams.Length == typeArguments.Length,
                "should have as many type parameters as type arguments");

            (bool success, var conceptParams, var associatedParams, var fixedMap) =
                PartitionTypeParameters(typeParams, typeArguments);
            if (!success)
            {
                // This instance has some unfixed non-witness/non-associated type
                // parameters.  We can't infer these, so give up on this
                // candidate instance.
                return default;
            }

            return InferWithClassifiedParameters(conceptParams, associatedParams, existingFixedMap.Compose(fixedMap), chain, ref useSiteDiagnostics);
        }

        /// <summary>
        /// Tries to infer a batch of concept witnesses given a fully
        /// classified set of type parameters and a map of previous type
        /// parameter fixings.
        /// </summary>
        /// <param name="conceptParams">
        /// The concept type parameters to infer.
        /// </param>
        /// <param name="associatedParams">
        /// The associated type parameters to infer.
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
        /// <param name="useSiteDiagnostics">
        /// An optional diagnostics bag used to report details on failing inference.
        /// </param>
        /// <returns>
        /// The final unification map if inference succeeded; the default map otherwise.
        /// </returns>
        internal ImmutableTypeMap InferWithClassifiedParameters(
            ImmutableArray<TypeParameterSymbol> conceptParams,
            ImmutableArray<TypeParameterSymbol> associatedParams,
            ImmutableTypeMap existingFixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // Early out: if we don't have any concept indices, we can't infer.
            if (conceptParams.IsEmpty)
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
                if (!conceptParams.IsEmpty)
                {
                    var newConceptParameters = TryInferConceptWitnesses(conceptParams, existingFixedMap, chain, ref currentSubstitution, ref useSiteDiagnostics);
                    conceptProgress = conceptProgress || (newConceptParameters.Length < conceptParams.Length);
                    conceptParams = newConceptParameters;
                }

                inferredAll = conceptParams.IsEmpty;

                // Stop if we made no progress whatsoever.
                if (!conceptProgress && !inferredAll)
                {
                    return default;
                }
            } while (!inferredAll);

            /* Once we've inferred all concept witnesses, all associated types
             * will either be fixed or not inferrable.  We double-check here.
             *
             * TODO: this is inefficient, as we do this substitution again
             *       later at the top level.  Some more lightweight way of
             *       checking to see if currentSubstitution has fixed
             *       allTypeParameters[i] might be better.
             */
            for (int i = 0; i < associatedParams.Length; i++)
            {
                if (currentSubstitution.SubstituteType(associatedParams[i]).AsTypeSymbolOnly() == associatedParams[i])
                {
                    return default;
                }
            }

            return currentSubstitution;
        }

        /// <summary>
        /// Tries to infer a batch of concept witnesses.
        /// </summary>
        /// <param name="conceptParameters">
        /// An array containing all type parameters that have been marked as
        /// witnesses to infer.
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
        /// <param name="useSiteDiagnostics">
        /// An optional diagnostics bag used to report details on failing inference.
        /// </param>
        /// <returns>
        /// An array of all concept-witness type parameters that have
        /// not been inferred this time.  These might become inferrable once
        /// some associated types have been fixed.
        /// </returns>
        private ImmutableArray<TypeParameterSymbol> TryInferConceptWitnesses(
            ImmutableArray<TypeParameterSymbol> conceptParameters,
            ImmutableTypeMap parentSubstitution,
            ImmutableHashSet<NamedTypeSymbol> chain,
            ref ImmutableTypeMap currentSubstitution,
            ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(currentSubstitution != null,
                "shouldn't have a null substitution here");

            var remainingConceptParameters = ArrayBuilder<TypeParameterSymbol>.GetInstance();

            for (var i = 0; i < conceptParameters.Length; i++)
            {
                var requiredConcepts = GetRequiredConceptsFor(conceptParameters[i], parentSubstitution);
                var candidate = InferOneWitnessFromRequiredConcepts(requiredConcepts, parentSubstitution, chain);

                if (!candidate.Viable)
                {
                    // Put this back on the inferring queue.
                    // It might need some other concepts to be inferred first.
                    remainingConceptParameters.Add(conceptParameters[i]);

                    // TODO: this diagnostics handling is almost certainly
                    //       wrong.
                    var diags = candidate.Diagnostics.AsImmutableOrNull();
                    if (useSiteDiagnostics != null && !diags.IsDefaultOrEmpty)
                    {
                        for (int j = 0; j < diags.Length; j++)
                        {
                            useSiteDiagnostics.Add(diags[j]);
                        }
                    }

                    continue;
                }

                // This concept has now been inferred, so we do two things.

                Debug.Assert(candidate.Instance.IsInstanceType() || candidate.Instance.IsConceptWitness,
                    "Concept witness inference returned something other than a concept instance or witness");

                /* 1) Add the inferred concept itself into the substitution.
                 *    This has to happen before we compose the unification,
                 *    to avoid accidentally clobbering the witness's
                 *    type parameter with any other like-named parameters
                 *    coming from recursive calls.
                 *    
                 *    TODO: why?
                 */
                currentSubstitution = currentSubstitution.Add(conceptParameters[i], new TypeWithModifiers(candidate.Instance));
                /* 2) Add its unification into our substitution, which will
                 *    propagate any associated types fixed by the concept.
                 *
                 *    This is a sequential composition, so order matters.
                 *    We must make sure that, if there are clashes on type
                 *    parameters, the outermost assignment wins.
                 */
                currentSubstitution = currentSubstitution.Compose(candidate.Unification);
            }

            return remainingConceptParameters.ToImmutableAndFree();
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
        internal Candidate InferOneWitnessFromRequiredConcepts(
            ImmutableArray<TypeSymbol> requiredConcepts,
            ImmutableTypeMap fixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain)
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
                var conceptDisplay = requiredConcepts.Length == 1 ? requiredConcepts[0].ToDisplayString() : "(multiple concepts)";
                // CS8957: No instances in scope satisfy concept '0'.
                var err = new CSDiagnosticInfo(ErrorCode.ERR_ConceptInstanceUnsatisfiable, conceptDisplay);
                var errs = new HashSet<DiagnosticInfo>{ err };
                return new Candidate(errs);
            }
            Debug.Assert(firstPassInstances.Length <= _allInstances.Length,
                "First pass of concept witness inference should not grow the instance list");

            var secondPassInstances = ToSatisfiableInstances(firstPassInstances, chain);
            if (secondPassInstances.IsDefaultOrEmpty)
            {
                var conceptDisplay = requiredConcepts.Length == 1 ? requiredConcepts[0].ToDisplayString() : "(multiple concepts)";
                // CS8957: No instances in scope satisfy concept '0'.
                var err = new CSDiagnosticInfo(ErrorCode.ERR_ConceptInstanceUnsatisfiable, conceptDisplay);
                var errs = new HashSet<DiagnosticInfo> { err };
                return new Candidate(errs);
            }
            Debug.Assert(secondPassInstances.Length <= firstPassInstances.Length,
                "Second pass of concept witness inference should not grow the instance list");

            // We only do tie breaking in the case of actual ties.
            var thirdPassInstances = secondPassInstances;
            if (1 < secondPassInstances.Length) thirdPassInstances = TieBreakInstances(secondPassInstances);
            Debug.Assert(thirdPassInstances.Length <= secondPassInstances.Length,
                "Third pass of concept witness inference should not grow the instance list");
            Debug.Assert(!thirdPassInstances.IsDefaultOrEmpty,
                "Third pass of concept witness inference should only break ties");
            if (thirdPassInstances.Length != 1)
            {
                var conceptDisplay = requiredConcepts.Length == 1 ? requiredConcepts[0].ToDisplayString() : "(multiple concepts)";

                // CS8958: Cannot infer a unique instance for concept '{0}'. For example, both '{1}' and '{2}' are valid instances.
                var err =
                    new CSDiagnosticInfo(
                        ErrorCode.ERR_ConceptInstanceAmbiguous,
                        conceptDisplay,
                        thirdPassInstances[0].Instance.ToDisplayString(),
                        thirdPassInstances[1].Instance.ToDisplayString());
                var errs = new HashSet<DiagnosticInfo> { err };
                return new Candidate(errs);
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
        private ImmutableArray<Candidate> ToSatisfiableInstances(
            ImmutableArray<Candidate> candidateInstances,
            ImmutableHashSet<NamedTypeSymbol> chain)
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

                /* We need to pass the old fixed map into the new recursion.
                 * Recursive inference using associated types seemingly
                 * can't be driven entirely from the fixed parameters of the
                 * candidate instance.
                 *
                 * TODO: understand exactly why this is.
                 * TODO: maybe we only need to keep the fixed map around, not
                 *       the whole unification?
                 */
                Candidate fixedInstance = InferRecursively(nt, candidate.Unification, chain.Add(nt));
                if (!fixedInstance.Viable)
                {
                    continue;
                }

                // Finally, check constraints on the fully fixed instance.
                // We do this here in case the constraint satisfaction depends
                // on the recursive inference.
                if (!fixedInstance.Instance.CheckAllConstraints(_conversions))
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
            ImmutableTypeMap existingFixedMap,
            ImmutableHashSet<NamedTypeSymbol> chain)
        {
            var diags = new HashSet<DiagnosticInfo>();
            var unification = Infer(unfixedInstance.TypeParameters, unfixedInstance.TypeArguments, existingFixedMap, chain, ref diags);
            if (unification == null)
            {
                // NB: diags might be empty at this stage (TODO: should it be?)
                return new Candidate(diags);
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
            var arb = new ArrayBuilder<Candidate>();
            foreach (var me in candidateInstances)
            {
                bool overlapped = false;
                foreach (var you in candidateInstances)
                {
                    if (me.Instance == you.Instance)
                    {
                        // An instance can't overlap itself!
                        continue;
                    }

                    if (!OverlapAllowed(overlapper: you, overlappee: me))
                    {
                        continue;
                    }

                    if (!ImplementsConceptsOf(implementee: you, implementor: me))
                    {
                        overlapped = true;
                        break;
                    }

                    if (ParamsLessSpecific(moreSpecific: you, lessSpecific: me))
                    {
                        overlapped = true;
                        break;
                    }

                    if (MoreWitnessMentions(moreMentions: you, fewerMentions: me))
                    {
                        overlapped = true;
                        break;
                    }
                }

                if (!overlapped)
                {
                    arb.Add(me);
                }
            }
            var ar = arb.ToImmutableAndFree();
            /* @MattWindsor91 (Concept-C# 2017)
             *
             * Sometimes, the above can eliminate _all_ instances.
             * In this case, we want to return the original set, since
             * no winners is the same as everyone winning.
             *
             * TODO: stop the elimination?
             */
            return ar.IsDefaultOrEmpty ? candidateInstances : ar;
        }

        /// <summary>
        /// Decides whether one candidate can kick another candidate out of
        /// consideration during tie-breaking.
        /// </summary>
        /// <param name="overlapper">
        /// The candidate that is more favourable and will win if overlapping
        /// is allowed.
        /// </param>
        /// <param name="overlappee">
        /// The candidate that is being overlapped.
        /// </param>
        /// <returns>
        /// True if the overlap is permitted (by overlapping-instance
        /// attributes); false otherwise.
        /// </returns>
        private static bool OverlapAllowed(Candidate overlapper, Candidate overlappee)
        {
            /* Overlap is permitted if:
             *
             * 1) the overlapping type is marked [Overlapping]; or
             * 2) the overlapped type is marked [Overlappable].
             *
             * TODO: type parameters?
             */
            var r = overlapper.Instance;
            var rOverlapping = r.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)r).IsOverlapping;
            if (rOverlapping)
            {
                return true;
            }

            var e = overlappee.Instance;
            var eOverlappable = e.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)e).IsOverlappable;
            return eOverlappable;
        }

        /// <summary>
        /// Checks whether one instance implements all of the concepts, either
        /// directly or through sub-concepts, of another instance.
        /// </summary>
        /// <param name="implementor">
        /// The instance that must implement all concepts of
        /// <paramref name="implementee"/>.
        /// </param>
        /// <param name="implementee">
        /// The instance whose concepts must be implemented.
        /// </param>
        /// <returns>
        /// True if, and only if, <paramref name="implementor"/> implements all of
        /// the concepts of <paramref name="implementee"/>.
        /// </returns>
        private static bool ImplementsConceptsOf(Candidate implementor, Candidate implementee)
        {
            Debug.Assert(implementor.Instance != implementee.Instance,
                "Shouldn't be checking an instance against itself!");

            var ignore = new HashSet<DiagnosticInfo>();

            foreach (var iface in implementee.Instance.AllInterfacesNoUseSiteDiagnostics)
            {
                if (!iface.IsConcept)
                {
                    continue;
                }

                var conceptImplemented = implementor.Instance.ImplementsInterface(iface, ref ignore);
                if (!conceptImplemented)
                {
                    return false;
                }
            }

            return true;
        }

        private static ImmutableArray<(NamedTypeSymbol, NamedTypeSymbol)> ProvidedMutualNamedConcepts(NamedTypeSymbol x, NamedTypeSymbol y)
        {
            var arb = ArrayBuilder<(NamedTypeSymbol, NamedTypeSymbol)>.GetInstance();
            foreach (var xc in x.AllInterfacesNoUseSiteDiagnostics)
            {
                if (xc.IsConcept)
                {
                    foreach (var yc in y.AllInterfacesNoUseSiteDiagnostics)
                    {
                        if (xc.OriginalDefinition == yc.OriginalDefinition)
                        {
                            arb.Add((xc, yc));
                        }
                    }
                }
            }
            return arb.ToImmutableAndFree();
        }

        /// <summary>
        /// Heuristically compares two type symbols to see if one is more
        /// 'specific' than the other as a concept type argument.
        /// </summary>
        /// <param name="x">The first symbol to compare.</param>
        /// <param name="y">The second symbol to compare.</param>
        /// <returns>
        /// A negative integer if <paramref name="x"/> is more specific;
        /// a positive integer if <paramref name="y"/> is more specific;
        /// zero if we can't make a decision either way.
        /// </returns>
        private static int CompareSpecificness(TypeSymbol x, TypeSymbol y)
        {
            // Named types are always more specific than type parameters.
            if (x.Kind == SymbolKind.NamedType && y.Kind == SymbolKind.TypeParameter)
            {
                return -1;
            }
            if (x.Kind == SymbolKind.TypeParameter && y.Kind == SymbolKind.NamedType)
            {
                return 1;
            }
            // We don't (currently) report on things other than NTs and params.
            // (TODO: tuples, etc)
            if (x.Kind != SymbolKind.NamedType || y.Kind != SymbolKind.NamedType)
            {
                return 0;
            }

            var xn = (NamedTypeSymbol)x;
            var yn = (NamedTypeSymbol)y;

            // Non-generic named types are always more specific than generic ones.
            var xg = xn.Arity == 0 ? -1 : 0;
            var yg = xn.Arity == 0 ? 1 : 0;

            // If x is generic, and y is not, the result will be -1 + 0 = -1.
            // If y is generic, and x is not, the result will be  0 + 1 =  1.
            return (xg + yg);
        }

        /// <summary>
        /// Decides whether an instance is strictly less specific than another.
        /// </summary>
        /// <param name="moreSpecific">
        /// The instance to compare against.
        /// </param>
        /// <param name="lessSpecific">
        /// The instance to compare.
        /// </param>
        /// <returns>
        /// True if, and only if, <paramref name="lessSpecific"/> is strictly less
        /// specific than <paramref name="moreSpecific"/>.
        /// </returns>
        private static bool ParamsLessSpecific(Candidate moreSpecific, Candidate lessSpecific)
        {
            Debug.Assert(moreSpecific.Instance != lessSpecific.Instance,
                "Shouldn't be checking an instance against itself!");

            var moreI = moreSpecific.Instance;
            if (moreI.Kind != SymbolKind.NamedType)
            {
                return false;
            }
            var moreN = (NamedTypeSymbol)moreI;

            var lessI = lessSpecific.Instance;
            if (lessI.Kind != SymbolKind.NamedType)
            {
                return false;
            }
            var lessN = (NamedTypeSymbol)lessI;

            bool atLeastOne = false;
            foreach (var (xc, yc) in ProvidedMutualNamedConcepts(lessN.OriginalDefinition, moreN.OriginalDefinition))
            {
                for (var i = 0; i < xc.Arity; i++)
                {
                    var cmp = CompareSpecificness(xc.TypeArguments[i], yc.TypeArguments[i]);
                    if (cmp < 0) // xc more specific
                    {
                        return false;
                    }
                    if (cmp > 0) // yc more specific
                    {
                        atLeastOne = true;
                    }
                }
            }
            return atLeastOne;
        }

        /// <summary>
        /// Decides whether one candidate mentions concept type arguments in
        /// its witness constraints more than another.
        /// </summary>
        /// <param name="moreMentions">
        /// The candidate that should be getting more mentions.
        /// </param>
        /// <param name="fewerMentions">
        /// The candidate that should be getting fewer mentions (and will be
        /// removed from tie-breaking if it does).
        /// </param>
        /// <returns>
        /// True if <paramref name="moreMentions"/> mentions at least one
        /// concept type argument more in its witness constraints than
        /// <paramref name="fewerMentions"/>, and no argument less.
        /// </returns>
        private static bool MoreWitnessMentions(Candidate moreMentions, Candidate fewerMentions)
        {
            // NOTE: This is not a very robust heuristic.
            //       Consider replacing with something more complete.

            void RecordTyparUsage(ref SmallDictionary<TypeParameterSymbol, int> counts, TypeSymbol witness)
            {
                if (witness.Kind != SymbolKind.TypeParameter)
                {
                    return;
                }
                var w = (TypeParameterSymbol)witness;

                foreach (var c in w.AllEffectiveInterfacesNoUseSiteDiagnostics)
                {
                    if (!c.IsConcept || c.Kind != SymbolKind.NamedType)
                    {
                        continue;
                    }

                    foreach (var t in c.TypeArguments)
                    {
                        if (t.Kind == SymbolKind.TypeParameter)
                        {
                            var tp = (TypeParameterSymbol)t;
                            counts[tp] = counts.ContainsKey(tp) ? counts[tp] + 1 : 1;
                        }
                    }
                }
            }

            // For this heuristic to work, both candidates must be named types
            // (so that they have type parameters).
            if (moreMentions.Instance.Kind != SymbolKind.NamedType ||
                fewerMentions.Instance.Kind != SymbolKind.NamedType)
            {
                return false;
            }

            // We're comparing the base instances here, not their fully fixed
            // forms.
            var mt = (NamedTypeSymbol)moreMentions.Instance.OriginalDefinition;
            var ft = (NamedTypeSymbol)fewerMentions.Instance.OriginalDefinition;

            var moreOnAtLeastOne = false;

            // Count the number of references each typar gets on each
            // side in a concept witness.
            // Overlap only occurs if, for _all_ concepts, for _all_
            // type parameters, 'more' gets more.
            var mcounts = new SmallDictionary<TypeParameterSymbol, int>();
            var fcounts = new SmallDictionary<TypeParameterSymbol, int>();

            for (var i = 0; i < mt.TypeArguments.Length; i++)
            {
                if (!mt.TypeParameters[i].IsConceptWitness)
                {
                    continue;
                }
                RecordTyparUsage(ref mcounts, mt.TypeArguments[i]);
            }
            for (var i = 0; i < ft.TypeArguments.Length; i++)
            {
                if (!ft.TypeParameters[i].IsConceptWitness)
                {
                    continue;
                }
                RecordTyparUsage(ref fcounts, ft.TypeArguments[i]);
            }

            // TODO: is it ok to look only at ft here?
            foreach (var fc in ft.AllInterfacesNoUseSiteDiagnostics)
            {
                if (!fc.IsConcept)
                {
                    continue;
                }

                foreach (var mc in mt.AllInterfacesNoUseSiteDiagnostics)
                {
                    if (fc.OriginalDefinition != mc.OriginalDefinition)
                    {
                        continue;
                    }

                    for (var j = 0; j < fc.TypeParameters.Length; j++)
                    {
                        // TODO: should we be handling named types specially?
                        if (mc.TypeArguments[j].Kind != SymbolKind.TypeParameter ||
                            fc.TypeArguments[j].Kind != SymbolKind.TypeParameter)
                        {
                            continue;
                        }

                        if (!mcounts.TryGetValue((TypeParameterSymbol)mc.TypeArguments[j], out var mtc))
                        {
                            mtc = 0;
                        }
                        if (!fcounts.TryGetValue((TypeParameterSymbol)fc.TypeArguments[j], out var ftc))
                        {
                            ftc = 0;
                        }
                        if (mtc < ftc)
                        {
                            return false;
                        }
                        if (ftc < mtc)
                        {
                            moreOnAtLeastOne = true;
                        }
                    }
                }
            }

            return moreOnAtLeastOne;
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
            var conceptParamsBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var associatedParamsBuilder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var fixedMap = new MutableTypeMap();
            int j = 0;
            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (typeParameters[i].IsConceptWitness)
                {
                    conceptParamsBuilder.Add(typeParameters[i]);
                }
                else if (typeParameters[i].IsAssociatedType)
                {
                    associatedParamsBuilder.Add(typeParameters[i]);
                }
                else
                {
                    allArguments[i] = typeArguments[j];
                    fixedMap.Add(typeParameters[i], new TypeWithModifiers(typeArguments[j]));
                    j++;
                }
            }

            var conceptParams = conceptParamsBuilder.ToImmutableAndFree();
            var associatedParams = associatedParamsBuilder.ToImmutableAndFree();

            // TODO: pass in diagnostics
            var ignore = new HashSet<DiagnosticInfo>();
            var unification = InferWithClassifiedParameters(conceptParams, associatedParams, fixedMap.ToUnification(), ImmutableHashSet<NamedTypeSymbol>.Empty, ref ignore);
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
                if (!expandAssociatedIfFailed || !conceptParams.IsEmpty)
                {
                    return ImmutableArray<TypeSymbol>.Empty;
                }

                // This will just cause each unfixed type parameter to bubble
                // through in the next section.
                unification = new ImmutableTypeMap();
            }

            /* As in normal inference, it is not necessarily a bug for
               the map to map typeParameters[i] to typeParameters[i].
               This might be, eg., a recursive call. */
            for (int i = 0; i < typeParameters.Length; i++)
            {
                if (allArguments[i] != null)
                {
                    continue;
                }
                allArguments[i] = unification.SubstituteType(typeParameters[i]).AsTypeSymbolOnly();
            }

            return allArguments.ToImmutableArray();
        }

        #endregion Part-inference
    }
}
