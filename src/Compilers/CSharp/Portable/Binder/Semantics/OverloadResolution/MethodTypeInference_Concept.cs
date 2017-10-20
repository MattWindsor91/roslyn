using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Text;
using System;
using Roslyn.Utilities;

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
        /// <returns>
        /// True if concept inference succeeded; false otherwise.
        /// </returns>
        private bool InferTypeArgsConceptPhase(Binder binder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(!AllFixed(),
                "Concept witness inference is pointless if there is nothing to infer");

            // Concept features disable if concept attributes aren't
            // present, and the semantics of concept inference depends
            // on these attributes being available anyway.
            if (!binder.Compilation.HasConceptAttributes)
            {
                return false;
            }

            var inferrer = new ConceptWitnessInferrer(binder);
            var methodInfo = MakeMethodInfo(inferrer);
            var fixedWithHeuristics = FixedArgsWithHeuristics(binder);
            var round = inferrer.NewRound(_methodTypeParameters, fixedWithHeuristics, new ImmutableTypeMap(), methodInfo);
            var result = round.Infer(ref useSiteDiagnostics);

            return FixFromResult(result);
        }

        /// <summary>
        /// Applies a witness inference result to this type inference question.
        /// </summary>
        /// <param name="result">
        /// The result to apply.
        /// </param>
        /// <returns>
        /// True if the witness inference succeeded (fixed all unfixed type
        /// parameters).
        /// Otherwise, false.
        /// </returns>
        private bool FixFromResult(ConceptWitnessInferrer.Result result)
        {
            var arity = _fixedResults.Length;
            Debug.Assert(0 < arity, "method should be generic if we got this far");

            // Even if concept inference failed, we want to log anything
            // we managed to fix for error reporting purposes.
            for (int i = 0; i < arity; i++)
            {
                if (_fixedResults[i] == null)
                {
                    // NB: this will set the slot to null if fixing failed.
                    _fixedResults[i] = result.TryFix(_methodTypeParameters[i]);
                }
            }

            return result.FixedEverything;
        }

        /// <summary>
        /// Generates a method info object for concept inference, using
        /// the method data from this round of method type inference.
        /// </summary>
        /// <param name="inf">The concept witness inferrer.</param>
        /// <returns>
        /// An appropriate <see cref="ConceptWitnessInferrer.MethodInfo"/>
        /// struct for this method.
        /// </returns>
        private ConceptWitnessInferrer.MethodInfo MakeMethodInfo(ConceptWitnessInferrer inf)
        {
            var unfixedParamsB = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            var initialFixedMap = new MutableTypeMap();
            for (var i = 0; i < _methodTypeParameters.Length; i++)
            {
                var tp = _methodTypeParameters[i];
                if (_fixedResults[i] != null)
                {
                    initialFixedMap.AddAndPropagate(_methodTypeParameters[i], new TypeWithModifiers(_fixedResults[i]));
                }
                else if (!tp.IsAssociatedType && !tp.IsConceptWitness)
                {
                    unfixedParamsB.Add(tp);
                }
            }
            var unfixedParams = unfixedParamsB.ToImmutableAndFree();
            // Make sure everything in the method info is as fully substituted
            // as possible.
            // TODO: slightly inefficient, but doesn't duplicate code.
            var methodInfo = inf.MakeMethodInfo(_constructedContainingTypeOfMethod, unfixedParams, _formalParameterTypes, _formalParameterRefKinds, _arguments);
            return methodInfo.ApplySubstitution(initialFixedMap);
        }

        /// <summary>
        /// Adds some 'best guesses' to the fixed arguments list to make
        /// concept inference run more smoothly.
        /// <para>
        /// This method is mainly a stopgap for improving concept based
        /// inference until we can think of a more robust way of adding
        /// the ideas in.
        /// </para>
        /// </summary>
        /// <param name="binder">
        /// The binder for the scope in which the type-inferred method
        /// resides.
        /// </param>
        /// <returns>
        /// The resulting fixed argument list, with some guesses.
        /// The guesses should be sound (ie, implicit conversions of anything
        /// that could possibly be inferred in their place), but not complete.
        /// </returns>
        private ImmutableArray<TypeSymbol> FixedArgsWithHeuristics(Binder binder)
        {
            var fixbuild = ArrayBuilder<TypeSymbol>.GetInstance();
            for (int i = 0; i < _methodTypeParameters.Length; i++)
            {
                if (_fixedResults[i] != null)
                {
                    fixbuild.Add(_fixedResults[i]);
                    continue;
                }
                // Heuristic:
                // 
                // If we couldn't fix a type parameter, but it corresponds to
                // a method group with only one viable method, fix it as the
                // corresponding Func<>.
                //
                // TODO(@MattWindsor91): move this into round 1 witness
                //     inference, and target the same delegate type as the
                //     provided concept.
                TypeSymbol fix = null;
                bool fixedAlready = false;
                for (int j = 0; j < _formalParameterTypes.Length; j++)
                {
                    if (_formalParameterTypes[j] == _methodTypeParameters[i]
                        && _arguments[j].Kind == BoundKind.MethodGroup)
                    {
                        var mg = (BoundMethodGroup)_arguments[j];
                        if (mg.Methods.Length != 1)
                        {
                            continue;
                        }

                        var method = mg.Methods[0];

                        TypeSymbol newfix = null;

                        newfix = method.ReturnsVoid ? MakeActionTypeFor(method, binder) : MakeFuncTypeFor(method, binder);
                        if (newfix == null)
                        {
                            continue;
                        }

                        if (!fixedAlready)
                        {
                            fix = newfix;
                            fixedAlready = true;
                        }
                        else if (fix != newfix)
                        {
                            // Unfix the parameter if there's a conflicting
                            // definition for it.
                            fix = null;
                        }
                    }
                }

                fixbuild.Add(fix);
            }
            var fixedWithHeuristics = fixbuild.ToImmutableAndFree();
            return fixedWithHeuristics;
        }

        /// <summary>
        /// Creates a Func type with the same parameter and return types
        /// as a given method symbol.
        /// </summary>
        /// <param name="method">
        /// The method symbol to create.  Must not return void.
        /// </param>
        /// <param name="binder">
        /// The binder for the scope in which the original type-inferred
        /// method resides.
        /// </param>
        /// <returns>
        /// A type symbol representing the corresponding Func if possible;
        /// null if one could not be made.
        /// </returns>
        private TypeSymbol MakeFuncTypeFor(MethodSymbol method, Binder binder)
        {
            Debug.Assert(method != null, "can't make func type for null method");
            Debug.Assert(!method.ReturnsVoid, "can't make a Func<> if the method returns void");

            var rtype = method.ReturnType;
            var funcwkt = WellKnownTypes.GetWellKnownFunctionDelegate(method.ParameterCount);
            if (funcwkt == WellKnownType.Unknown)
            {
                return null;
            }

            var functype = binder.Compilation.GetWellKnownType(funcwkt);
            if (functype.HasUseSiteError)
            {
                return null;
            }

            var ftargs = new TypeSymbol[method.ParameterCount + 1];
            for (var k = 0; k < method.ParameterCount; k++)
            {
                ftargs[k] = method.ParameterTypes[k];
            }
            ftargs[method.ParameterCount] = method.ReturnType;

            return functype.Construct(ftargs);
        }


        /// <summary>
        /// Creates an Action type with the same parameter and return types
        /// as a given method symbol.
        /// </summary>
        /// <param name="method">
        /// The method symbol to create.  Must return void.
        /// </param>
        /// <param name="binder">
        /// The binder for the scope in which the original type-inferred
        /// method resides.
        /// </param>
        /// <returns>
        /// A type symbol representing the corresponding Func if possible;
        /// null if one could not be made.
        /// </returns>
        private TypeSymbol MakeActionTypeFor(MethodSymbol method, Binder binder)
        {
            Debug.Assert(method != null, "can't make action type for null method");
            Debug.Assert(method.ReturnsVoid, "can't make an Action<> if the method returns void");

            var actwkt = WellKnownTypes.GetWellKnownActionDelegate(method.ParameterCount);
            if (actwkt == WellKnownType.Unknown)
            {
                return null;
            }

            var acttype = binder.Compilation.GetWellKnownType(actwkt);
            if (acttype.HasUseSiteError)
            {
                return null;
            }

            return acttype.Construct(method.ParameterTypes);
        }
    }

    /// <summary>
    /// An object that, given a series of viable instances and bound type
    /// parameters, can perform concept witness inference.
    /// </summary>
    internal class ConceptWitnessInferrer
    {
        /// <summary>
        /// The binder at the scope in which we are inferring witnesses.
        /// </summary>
        private readonly Binder _binder;

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
        private readonly ImmutableHashSet<TypeParameterSymbol> _rigidParams;

        /// <summary>
        /// The available conversions in scope.
        /// </summary>
        private readonly ConversionsBase _conversions;

        /// <summary>
        /// Information about the results of a concept inference round.
        /// </summary>
        public struct Result
        {
            /// <summary>
            /// The set of associated type parameters that were not fixed
            /// by the time we ended inference.
            /// <para>
            /// If null or empty, there were no unfixed associated type
            /// parameters.
            /// </para>
            /// </summary>
            private readonly ImmutableHashSet<TypeParameterSymbol> _unfixedAssocs;

            /// <summary>
            /// The set of concept witness type parameters that were not fixed
            /// by the time we ended inference.
            /// <para>
            /// If null or empty, there were no unfixed concept witness type
            /// parameters.
            /// </para>
            /// </summary>
            private readonly ImmutableHashSet<TypeParameterSymbol> _unfixedConcepts;

            /// <summary>
            /// The set of method-level parameters that were not fixed
            /// by the time we ended inference.
            /// </summary>
            private readonly ImmutableArray<TypeParameterSymbol> _unfixedMethodParams;

            /// <summary>The map of inferred fixings.</summary>
            private readonly ImmutableTypeMap _fixedMap;

            /// <summary>The map of inferred fixings.</summary>
            internal ImmutableTypeMap Map => _fixedMap;

            /// <summary>
            /// Any type parameters that failed initial classification.
            /// </summary>
            private readonly ImmutableArray<TypeParameterSymbol> _unclassifiableParams;

            /// <summary>
            /// Creates a result.
            /// <para>
            /// The result is successful if there were no unfixed
            /// associated types, concepts, or method parameters.
            /// </para>
            /// </summary>
            /// <param name="unfixedAssocs">
            /// Any unfixed associated type parameters.
            /// </param>
            /// <param name="unfixedConcepts">
            /// Any unfixed concept witness parameters.
            /// </param>
            /// <param name="unfixedMethodParams">
            /// Any unfixed method-level type parameters.
            /// </param>
            /// <param name="fixedMap">
            /// The map of all fixes made during concept type inference.
            /// </param>
            /// <param name="unclassifiableParams">
            /// Optionally, any type parameters that failed initial
            /// classification.
            /// </param>
            internal Result(
                ImmutableHashSet<TypeParameterSymbol> unfixedAssocs,
                ImmutableHashSet<TypeParameterSymbol> unfixedConcepts,
                ImmutableArray<TypeParameterSymbol> unfixedMethodParams,
                ImmutableTypeMap fixedMap,
                ImmutableArray<TypeParameterSymbol> unclassifiableParams = default
            )
            {
                Debug.Assert(unfixedAssocs != null,
                    "unfixed associated type set must not be null");
                Debug.Assert(unfixedConcepts != null,
                    "unfixed concept set must not be null");
                Debug.Assert(fixedMap != null,
                    "fixed map must not be null");

                _unfixedAssocs = unfixedAssocs;
                _unfixedConcepts = unfixedConcepts;
                _unfixedMethodParams = unfixedMethodParams;
                _fixedMap = fixedMap;
                _unclassifiableParams = unclassifiableParams;
            }

            /// <summary>
            /// Checks whether this result left the given type parameter
            /// unfixed.
            /// <para>
            /// This happens when the parameter is an associated type that
            /// might be inferrable through another round of normal method
            /// type inference, or it is a concept witness that might be
            /// inferrable by fixing said associated types.
            /// </para>
            /// </summary>
            /// <param name="t">The type parameter to check.</param>
            /// <returns>
            /// True if <paramref name="t"/> has been left unfixed;
            /// false otherwise.
            /// </returns>
            private bool LeftUnfixed(TypeParameterSymbol t)
            {
                if (_unfixedAssocs.Contains(t))
                {
                    return true;
                }
                if (_unfixedConcepts.Contains(t))
                {
                    return true;
                }
                if (!_unfixedMethodParams.IsDefaultOrEmpty && _unfixedMethodParams.Contains(t))
                {
                    return true;
                }
                return (!_unclassifiableParams.IsDefaultOrEmpty && _unclassifiableParams.Contains(t));
            }

            /// <summary>
            /// Tries to fix the given type parameter using this result.
            /// </summary>
            /// <param name="t">The type parameter to fix.</param>
            /// <returns>
            /// The resulting type symbol, if this result fixed
            /// <paramref name="t"/>.
            /// Otherwise, null.
            /// </returns>
            public TypeSymbol TryFix(TypeParameterSymbol t)
            {
                if (LeftUnfixed(t))
                {
                    return null;
                }
                // One might be tempted to put in an assertion here that
                // the map can't return t.  However, sometimes it can!
                // For example, consider
                //
                // private static void Qsort<T, implicit OrdT>
                //     (T[] xs, int lo, int hi) where OrdT : Ord<T>
                // {
                //     if (lo < hi)
                //     {
                //         var p = Partition(xs, lo, hi);
                //         Qsort(xs, lo, p - 1);
                //         Qsort(xs, p + 1, hi);
                //     }
                // }
                //
                // In this case, the map will resolve the missing
                // type parameters in each Qsort call as T->T, OrdT->OrdT.
                // These are exactly the same symbols.
                return _fixedMap.SubstituteType(t).AsTypeSymbolOnly();
            }

            /// <summary>
            /// Gets whether this result fixed everything, including
            /// missing method parameters.
            /// </summary>
            public bool FixedEverything => Success && _unfixedMethodParams.IsDefaultOrEmpty;

            /// <summary>
            /// Gets whether this result succeeded (fixed all
            /// associated types and concepts, and didn't immediately
            /// fail due to unexpected unfixed type parameters).
            /// </summary>
            public bool Success
            {
                get
                {
                    if (!_unfixedAssocs.IsEmpty)
                    {
                        return false;
                    }
                    if (!_unfixedConcepts.IsEmpty)
                    {
                        return false;
                    }
                    return _unclassifiableParams.IsDefaultOrEmpty;
                }
            }
        }

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
        /// A type parameter wrapper that changes the ordinal of the underlying
        /// parameter.
        /// </summary>
        /// <remarks>
        /// This class exists solely to sidestep an issue in recursively calling
        /// method type inference: MTI checks each type parameter's ordinal
        /// against the length of the type parameter list.  Since our recursive
        /// calls shrink the parameter list, we need to renumber the parameters.
        /// 
        /// It's probably not safe to use this symbol in any context other than
        /// quickfixing this use of MTI, and ideally we wouldn't need to use it
        /// anyway after moving concept inference into phase 2.
        /// </remarks>
        private class RenumberedTypeParameterSymbol : WrappedTypeParameterSymbol
        {
            /// <summary>
            /// Constructs a new renumbered type parameter.
            /// </summary>
            /// <param name="original">
            /// The original type parameter.
            /// </param>
            /// <param name="newOrdinal">
            /// The new ordinal to report for this type parameter.
            /// </param>
            public RenumberedTypeParameterSymbol(TypeParameterSymbol original, int newOrdinal)
                : base(underlyingTypeParameter: original)
            {
                _ordinal = newOrdinal;
            }

            private int _ordinal;
            public override int Ordinal => _ordinal;

            public override Symbol ContainingSymbol => UnderlyingTypeParameter.ContainingSymbol;
            internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
                => UnderlyingTypeParameter.GetConstraintTypes(inProgress);
            internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
                => UnderlyingTypeParameter.GetDeducedBaseType(inProgress);
            internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
                => UnderlyingTypeParameter.GetEffectiveBaseClass(inProgress);
            internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
                => UnderlyingTypeParameter.GetInterfaces(inProgress);
        }

        /// <summary>
        /// Context for using information about the method of a concept
        /// witness inference pass to fix unfixed type parameters.
        /// </summary>
        internal abstract class MethodInfo
        {
            /// <summary>
            /// Is the given type parameter an unfixed normal parameter
            /// on the method, if any, represented by this object?
            /// </summary>
            /// <param name="tp">
            /// The type parameter to check.
            /// </param>
            /// <returns>
            /// True IFF <paramref name="tp"/> is unfixed, comes from the
            /// method this object represents, and is neither an associated
            /// type nor a concept witness.
            /// </returns>
            public abstract bool IsUnfixed(TypeParameterSymbol tp);

            /// <summary>
            /// Does the method, if any, represented by this object
            /// have any remaining unfixed normal type parameters?
            /// </summary>
            public abstract bool HasUnfixed { get; }

            /// <summary>
            /// The list of normal method type parameters still left to fix.
            /// </summary>
            public abstract ImmutableArray<TypeParameterSymbol> Unfixed { get; }

            /// <summary>
            /// Applies a substitution to the type parameters inside this
            /// method info, letting them be used in any future type
            /// inference calls.
            /// </summary>
            /// <param name="substitution">
            /// The substitution to apply.
            /// </param>
            /// <returns>
            /// The new method info resulting from the substitution.
            /// This info object is not modified.
            /// </returns>
            public abstract MethodInfo ApplySubstitution(AbstractTypeMap substitution);

            /// <summary>
            /// Performs a recursive round of method type inference to try
            /// fix normal, unfixed, method-level type parameters.
            /// </summary>
            /// <param name="useSiteDiagnostics">
            /// A diagnostics set to populate with any diagnostics from the
            /// recursive inference.
            /// </param>
            /// <returns>
            /// A Boolean that is true IFF at least one fixing occurred,
            /// and the map of fixings created by inference.
            /// </returns>
            public abstract (bool madeProgress, ImmutableTypeMap fixedMap) DoMethodTypeInference(ref HashSet<DiagnosticInfo> useSiteDiagnostics);
        }

        /// <summary>
        /// Immutable object representing the lack of method information
        /// during a standalone or part-infer concept inference pass.
        /// </summary>
        internal class AbsentMethodInfo : MethodInfo
        {
            private readonly ImmutableTypeMap _emptyMap = new ImmutableTypeMap();

            public override bool IsUnfixed(TypeParameterSymbol tp) => false;
            public override bool HasUnfixed => false;
            public override ImmutableArray<TypeParameterSymbol> Unfixed => ImmutableArray<TypeParameterSymbol>.Empty;
            public override MethodInfo ApplySubstitution(AbstractTypeMap substitution) => this;
            public override (bool madeProgress, ImmutableTypeMap fixedMap) DoMethodTypeInference(ref HashSet<DiagnosticInfo> useSiteDiagnostics) => (false, _emptyMap);
        }

        /// <summary>
        /// Immutable object containing information about the parent method
        /// of a pass of concept inference.
        /// </summary>
        internal class PresentMethodInfo : MethodInfo
        {
            private readonly Binder _binder;
            private readonly NamedTypeSymbol _containerType;
            private readonly ImmutableArray<TypeParameterSymbol> _unfixedTypeParams;
            private readonly ImmutableArray<TypeSymbol> _formalParameterTypes;
            private readonly ImmutableArray<RefKind> _formalParameterRefKinds;
            private readonly ImmutableArray<BoundExpression> _arguments;
            private readonly ImmutableHashSet<TypeParameterSymbol> _rigidParams;

            public PresentMethodInfo(
                Binder binder,
                NamedTypeSymbol containerType,
                ImmutableArray<TypeParameterSymbol> unfixedTypeParams,
                ImmutableArray<TypeSymbol> formalParameterTypes,
                ImmutableArray<RefKind> formalParameterRefKinds,
                ImmutableArray<BoundExpression> arguments,
                ImmutableHashSet<TypeParameterSymbol> rigidParams)
            {
                Debug.Assert(binder != null, $"{nameof(binder)} cannot be null");
                _binder = binder;

                Debug.Assert(containerType != null, $"{nameof(containerType)} cannot be null");
                _containerType = containerType;

                _unfixedTypeParams = unfixedTypeParams;
                _formalParameterTypes = formalParameterTypes;
                _formalParameterRefKinds = formalParameterRefKinds;
                _arguments = arguments;
                _rigidParams = rigidParams;
            }

            public override bool IsUnfixed(TypeParameterSymbol tp) =>
                _unfixedTypeParams.Contains(tp);

            public override bool HasUnfixed => !_unfixedTypeParams.IsEmpty;

            public override ImmutableArray<TypeParameterSymbol> Unfixed => _unfixedTypeParams;

            public override MethodInfo ApplySubstitution(AbstractTypeMap substitution)
            {
                Debug.Assert(substitution != null,
                    "shouldn't be applying a null substitution");

                var newUnfixedTypeParams = SubstituteUnfixed(substitution);
                var newContainerType = substitution.SubstituteNamedType(_containerType);
                var newFormalParameterTypes = SubstituteFormals(substitution);

                return new PresentMethodInfo(
                    _binder,
                    newContainerType,
                    newUnfixedTypeParams,
                    newFormalParameterTypes,
                    _formalParameterRefKinds,
                    _arguments,
                    _rigidParams
                );
            }

            private ImmutableArray<TypeParameterSymbol> SubstituteUnfixed(AbstractTypeMap substitution)
            {
                Debug.Assert(substitution != null,
                    "shouldn't apply a null substitution");

                if (_unfixedTypeParams.IsEmpty)
                {
                    return _unfixedTypeParams;
                }

                var newUnfixedTypeParamsB = ArrayBuilder<TypeParameterSymbol>.GetInstance();
                foreach (var unfixedTypeParam in _unfixedTypeParams)
                {
                    var subst = substitution.SubstituteType(unfixedTypeParam).AsTypeSymbolOnly();
                    Debug.Assert(subst != null, "substitution teturned null");

                    var haveFixed = subst != unfixedTypeParam && TypeArgumentIsFixed(subst, _rigidParams);
                    if (!haveFixed)
                    {
                        Debug.Assert(subst.Kind == SymbolKind.TypeParameter || subst.Kind == SymbolKind.ErrorType,
                            "allegedly unfixed substitution was neither a TP nor an error");

                        // If the substitution mapped us to *another* unfixed
                        // type parameter, we should fix that instead.
                        var newUnfixedTypeParam =
                            subst.Kind == SymbolKind.TypeParameter
                            ? (TypeParameterSymbol)subst
                            : unfixedTypeParam;
                        newUnfixedTypeParamsB.Add(newUnfixedTypeParam);
                    }
                }
                return newUnfixedTypeParamsB.ToImmutableAndFree();
            }

            private ImmutableArray<TypeSymbol> SubstituteFormals(AbstractTypeMap substitution)
            {
                Debug.Assert(substitution != null,
                    "shouldn't be applying a null substitution");

                if (_formalParameterTypes.IsDefaultOrEmpty)
                {
                    return _formalParameterTypes;
                }

                var newFormalParameterTypesB = ArrayBuilder<TypeSymbol>.GetInstance();
                foreach (var formalParameterType in _formalParameterTypes)
                {
                    newFormalParameterTypesB.Add(substitution.SubstituteType(formalParameterType).AsTypeSymbolOnly());
                }

                return newFormalParameterTypesB.ToImmutableAndFree();
            }

            public override (bool madeProgress, ImmutableTypeMap fixedMap) DoMethodTypeInference(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            {
                // Early out: if we don't have unfixed types, we made no
                // progress.
                if (_unfixedTypeParams.IsEmpty)
                {
                    return (false, new ImmutableTypeMap());
                }

                // MethodTypeInferrer refuses to bound type parameters with
                // ordinals that are outside the bounds of the type parameter
                // list.  We removed some type parameters to pose this
                // inference question, so the ordinals may now be invalid.
                // We cheat here by renumbering the ordinals.
                var rnTyparsB = ArrayBuilder<TypeParameterSymbol>.GetInstance();
                var rnTyparsM = new MutableTypeMap();
                for (int i = 0; i < _unfixedTypeParams.Length; i++)
                {
                    var original = _unfixedTypeParams[i];
                    var renumbered = new RenumberedTypeParameterSymbol(original, i);
                    rnTyparsB.Add(renumbered);
                    rnTyparsM.Add(original, new TypeWithModifiers(renumbered));
                }
                var rnTypars = rnTyparsB.ToImmutableAndFree();
                var rnFormals = SubstituteFormals(rnTyparsM);
                var rnContainer = rnTyparsM.SubstituteNamedType(_containerType);
                
                var mti = MethodTypeInferrer.Infer(_binder, rnTypars, rnContainer, rnFormals, _formalParameterRefKinds, _arguments, ref useSiteDiagnostics);

                // If we failed, we might have still inferred some arguments.
                // We assume any argument that isn't error-type, and that
                // is not the original parameter, is valid.
                var madeProgress = false;
                var fixedMapB = new MutableTypeMap();
                for (var i = 0; i < _unfixedTypeParams.Length; i++)
                {
                    var infarg = mti.InferredTypeArguments[i];
                    if (infarg != null && infarg.Kind != SymbolKind.ErrorType && infarg != rnTypars[i])
                    {
                        fixedMapB.AddAndPropagate(_unfixedTypeParams[i], new TypeWithModifiers(infarg));
                        madeProgress = true;
                    }
                }

                return (madeProgress, fixedMapB.ToUnification());
            }
        }

        /// <summary>
        /// Constructs a new ConceptWitnessInferrer with method information.
        /// <para>
        /// If the method information is present and correct, the concept
        /// witness inferrer will perform sub-rounds of method type inference
        /// to try fix unfixed method type parameters.
        /// </para>
        /// </summary>
        /// <param name="binder">
        /// The binder providing scope for the new inferrer.
        /// </param>
        public ConceptWitnessInferrer(Binder binder)
        {
            // We need two things from the outer scope:
            // 1) All instances visible to this method call;
            // 2) All type parameters bound in the method and class.
            // For efficiency, we do these in one go.
            // TODO: Ideally this should be cached at some point, perhaps on the
            // compilation or binder.
            (var allInstances, var rigidParams) = SearchScopeForInstancesAndParams(binder);
            _allInstances = allInstances;
            _rigidParams = rigidParams;
            _conversions = binder.Conversions;
            _binder = binder;
        }

        #region Setup from binder

        public PresentMethodInfo MakeMethodInfo(
            NamedTypeSymbol containerType,
            ImmutableArray<TypeParameterSymbol> unfixedTypeParams,
            ImmutableArray<TypeSymbol> formalParameterTypes,
            ImmutableArray<RefKind> formalParameterRefKinds,
            ImmutableArray<BoundExpression> arguments) =>
            new PresentMethodInfo(_binder, containerType, unfixedTypeParams, formalParameterTypes, formalParameterRefKinds, arguments, _rigidParams);

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
                b.GetConceptInstances(Binder.ConceptSearchOptions.SearchContainers | Binder.ConceptSearchOptions.SearchUsings, iBuilder, binder, ref ignore);
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
            if (container.Kind != SymbolKind.Method && container.Kind != SymbolKind.NamedType)
            {
                return;
            }

            ImmutableArray<TypeParameterSymbol> tps = GetTypeParametersOf(container);
            foreach (var tp in tps)
            {
                if (tp.IsConceptWitness)
                {
                    instances.Add(tp);
                }
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
            if (container.Kind != SymbolKind.Namespace && container.Kind != SymbolKind.NamedType)
            {
                return;
            }

            foreach (var member in ((NamespaceOrTypeSymbol)container).GetTypeMembers())
            {
                if (!binder.IsAccessible(member, ref ignore, binder.ContainingType))
                {
                    continue;
                }

                // Assuming that instances don't contain sub-instances.
                if (member.IsInstance)
                {
                    instances.Add(member);
                }
            }
        }

        #endregion Setup from binder
        #region Main driver


        /// <summary>
        /// Creates an infer round from a set of parameters and arguments.
        /// </summary>
        /// <param name="typeParameters">
        /// The set of type parameters being inferred.
        /// </param>
        /// <param name="typeArguments">
        /// The set of already-inferred type arguments; unfixed parameters must
        /// either be represented by a null, or a copy of the corresponding
        /// type parameter.
        /// </param>
        /// <param name="existingFixedMap">
        /// The existing fixed map, to extend with any fixings from these type
        /// parameters and arguments.
        /// </param>
        /// <param name="methodInfoOpt">
        /// Information about the containing method, if this is a method-level
        /// type inference request.
        /// </param>
        /// <param name="chainOpt">
        /// The set of previously seen concept instances, if this is a
        /// recursive round.
        /// </param>
        /// <returns>
        /// An <see cref="InferRound"/> class that can be used to perform
        /// inference.
        /// </returns>
        internal InferRound NewRound(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeSymbol> typeArguments,
            ImmutableTypeMap existingFixedMap,
            MethodInfo methodInfoOpt = default,
            ImmutableHashSet<NamedTypeSymbol> chainOpt = null)
        {
            Debug.Assert(typeParameters.Length == typeArguments.Length,
                "There should be as many type parameters as arguments.");

            var witnessB = ImmutableHashSet.CreateBuilder<TypeParameterSymbol>();
            var assocB = ImmutableHashSet.CreateBuilder<TypeParameterSymbol>();
            var fixedMapB = new MutableTypeMap();

            for (int i = 0; i < typeParameters.Length; i++)
            {
                var arg = typeArguments[i];
                var par = typeParameters[i];

                if (TypeArgumentIsFixed(arg, _rigidParams))
                {
                    // TODO(@MattWindsor91): unfixed params?
                    fixedMapB.Add(par, new TypeWithModifiers(arg));
                }
                // arg might not be null, eg. it might have been set to par.
                else if (par.IsConceptWitness)
                {
                    witnessB.Add(par);
                }
                else if (par.IsAssociatedType)
                {
                    assocB.Add(par);
                }
                else if (!methodInfoOpt.IsUnfixed(par))
                {
                    // If we got here, we have an unexpected unfixed type parameter.
                    return new FailedInferRound(typeParameters, existingFixedMap);
                }
            }

            return new NormalInferRound(
                parent: this,
                conceptWitnesses: witnessB.ToImmutable(),
                associatedTypes: assocB.ToImmutable(),
                fixedMap: existingFixedMap.Compose(fixedMapB.ToUnification()),
                methodInfo: methodInfoOpt,
                chain: chainOpt ?? ImmutableHashSet<NamedTypeSymbol>.Empty);
        }

        /// <summary>
        /// Decides whether a given type argument is fixed (successfully
        /// inferred).
        /// </summary>
        /// <param name="typeArgument">The type argument to check.</param>
        /// <param name="rigidParams">The rigid type parameters.</param>
        /// <returns>
        /// True if the argument is fixed.  This method may sometimes
        /// return false negatives, which affects completeness
        /// (some valid type inference may fail) but not soundness.
        /// </returns>
        internal static bool TypeArgumentIsFixed(TypeSymbol typeArgument, ImmutableHashSet<TypeParameterSymbol> rigidParams)
        {
            // TODO(@MattWindsor91):
            //   This is slightly ad-hoc and needs checking.
            //   The intuition is that:
            //   1) In some places (eg. method inference), unfixed type
            //      arguments are always null, so we can just check for null.
            if (typeArgument == null)
            {
                return false;
            }
            //   2) In other places, they are some type parameter.
            if (typeArgument.Kind != SymbolKind.TypeParameter && typeArgument.Kind != SymbolKind.ErrorType)
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
            return rigidParams.Contains(typeArgument as TypeParameterSymbol);
        }

        /// <summary>A single recursion through concept inference.</summary>
        internal abstract class InferRound
        {
            /// <summary>The current fixed map at this level.</summary>
            protected ImmutableTypeMap _fixedMap;

            /// <summary>Internal constructor for inference rounds.</summary>
            /// <param name="existingFixedMap">
            /// The fixed map at the start of this inference round.
            /// </param>
            protected InferRound(ImmutableTypeMap existingFixedMap)
            {
                _fixedMap = existingFixedMap;
            }

            /// <summary>
            /// Performs inference for this round.
            /// <para>
            /// Inference is destructive and not re-entrant.  To infer again,
            /// create a new round from the parent inferrer.
            /// </para>
            /// </summary>
            /// <returns>
            /// The <see cref="Result"/> summarising the progress made on inference.
            /// </returns>
            public abstract Result Infer(ref HashSet<DiagnosticInfo> useSiteDiagnostics);
        }

        /// <summary>
        /// Dummy object representing an infer round where some type parameters
        /// are not inferrable.
        /// </summary>
        internal class FailedInferRound : InferRound
        {
            private ImmutableArray<TypeParameterSymbol> _unclassifiableParams;

            /// <summary>
            /// Creates a failed infer round.
            /// </summary>
            /// <param name="unclassifiableParams">
            /// The parameters that were supplied to this inference question.
            /// </param>
            /// <param name="existingFixedMap">
            /// The fixed map at the start of inference.
            /// </param>
            public FailedInferRound(ImmutableArray<TypeParameterSymbol> unclassifiableParams, ImmutableTypeMap existingFixedMap)
                : base(existingFixedMap)
            {
                _unclassifiableParams = unclassifiableParams;
            }

            public override Result Infer(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            {
                return new Result(
                    unfixedAssocs: ImmutableHashSet<TypeParameterSymbol>.Empty,
                    unfixedConcepts: ImmutableHashSet<TypeParameterSymbol>.Empty,
                    unfixedMethodParams: ImmutableArray<TypeParameterSymbol>.Empty,
                    fixedMap: _fixedMap,
                    unclassifiableParams: _unclassifiableParams
                );
            }
        }

        /// <summary>
        /// Mutable object representing a single round of concept inference.
        /// </summary>
        internal class NormalInferRound : InferRound
        {
            /// <summary>
            /// The concept inferrer that created this round.
            /// </summary>
            private ConceptWitnessInferrer _parent;

            /// <summary>
            /// The set of previously explored concept parameters, used as a
            /// simple bound on termination.
            /// </summary>
            private ImmutableHashSet<NamedTypeSymbol> _chain;

            /// <summary>
            /// The current set of associated types needing to be inferred.
            /// This shrinks as progress is made.
            /// </summary>
            private ImmutableHashSet<TypeParameterSymbol> _associatedTypes;

            /// <summary>
            /// The current set of concept witnesses needing to be inferred.
            /// This shrinks as progress is made.
            /// </summary>
            private ImmutableHashSet<TypeParameterSymbol> _conceptWitnesses;

            /// <summary>
            /// Optional information about the underlying method, if this round
            /// belongs to a method type inference run.
            /// </summary>
            private MethodInfo _methodInfo;

            public NormalInferRound(
                ConceptWitnessInferrer parent,
                ImmutableHashSet<TypeParameterSymbol> conceptWitnesses,
                ImmutableHashSet<TypeParameterSymbol> associatedTypes,
                ImmutableTypeMap fixedMap,
                MethodInfo methodInfo,
                ImmutableHashSet<NamedTypeSymbol> chain)
                : base(fixedMap)
            {
                _parent = parent;
                _conceptWitnesses = conceptWitnesses;
                _associatedTypes = associatedTypes;
                _methodInfo = methodInfo;
                _chain = chain;
            }

            public override Result Infer(ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            {
                if (_conceptWitnesses.IsEmpty)
                {
                    // Can't do any inference without witnesses.
                    return AsResult();
                }

                // TODO(@MattWindsor91):
                //   This round of MTI should not be necessary unless we're
                //   coming from a recursive call, but currently MTI seems to
                //   leave some things uninferred that can be inferred with a
                //   second call.
                //
                //   I suspect this is the second phase early-outing because of
                //   associated types/concept witnesses but haven't been able
                //   to investigate yet.

                // For the recursive call, we must make sure that any
                // fixings that happened when unifying for this instance
                // are propagated to the method, and that any possible
                // method type inference is done.
                // Failing to do so means that, when we start inferring
                // instances, we're missing some of the type information
                // we might need.
                _methodInfo = _methodInfo.ApplySubstitution(_fixedMap);
                (var initMethodProgress, var initMethodSubstitution) = _methodInfo.DoMethodTypeInference(ref useSiteDiagnostics);
                if (initMethodProgress)
                {
                    _fixedMap = _fixedMap.Compose(initMethodSubstitution);
                    PropagateFixedMap();
                    // If we don't do this, we'll think the type parameters we
                    // just fixed through MTI are unfixable, since they're no
                    // longer present in the method info.
                    //unfixedInstance = initMethodSubstitution.SubstituteNamedType(unfixedInstance);
                }


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
                do
                {
                    var conceptProgress = false;

                    // Can't build this in-place due to the foreach.
                    var remainingConceptsB = ImmutableHashSet.CreateBuilder<TypeParameterSymbol>();
                    foreach (var conceptWitness in _conceptWitnesses)
                    {
                        if (InferWitness(conceptWitness, ref useSiteDiagnostics))
                        {
                            conceptProgress = true;
                        }
                        else
                        {
                            remainingConceptsB.Add(conceptWitness);
                        }
                    }

                    if (conceptProgress)
                    {
                        _conceptWitnesses = remainingConceptsB.ToImmutable();
                        PropagateFixedMap();
                    }
                    // If we didn't make any progress this round, but we're
                    // in a method and have unfixed type parameters, try a
                    // round of normal method type inference.
                    // Since this recursive MTI won't try to fix any concepts,
                    // it should terminate.
                    else
                    {
                        (var mtiProgress, var methodSubstitution) = _methodInfo.DoMethodTypeInference(ref useSiteDiagnostics);
                        conceptProgress |= mtiProgress;
                        _fixedMap = _fixedMap.Compose(methodSubstitution);
                        PropagateFixedMap();
                    }

                    if (!conceptProgress)
                    {
                        break;
                    }
                } while (!FixedEverything);

                return AsResult();
            }

            /// <summary>Converts this inference round to a result.</summary>
            /// <returns>A <see cref="Result"/> for this round.</returns>
            private Result AsResult() =>
                new Result(
                    unfixedAssocs: _associatedTypes,
                    unfixedConcepts: _conceptWitnesses,
                    unfixedMethodParams: _methodInfo.Unfixed,
                    fixedMap: _fixedMap
                );

            /// <summary>True if all type parameters have been fixed.</summary>
            private bool FixedEverything
            {
                get
                {
                    if (_methodInfo.HasUnfixed)
                    {
                        return false;
                    }
                    return _associatedTypes.IsEmpty && _conceptWitnesses.IsEmpty;
                }
            }

            /// <summary>The type parameters we still need to fix.</summary>
            private ImmutableArray<TypeParameterSymbol> ToFix
            {
                get
                {
                    if (FixedEverything)
                    {
                        return ImmutableArray<TypeParameterSymbol>.Empty;
                    }
                    var builder = ArrayBuilder<TypeParameterSymbol>.GetInstance();
                    builder.AddRange(_methodInfo.Unfixed);
                    builder.AddRange(_associatedTypes);
                    builder.AddRange(_conceptWitnesses);
                    return builder.ToImmutableAndFree();
                }
            }

            /// <summary>
            /// Updates the method context and associated types with the
            /// current substitution.
            /// </summary>
            private void PropagateFixedMap()
            {
                _methodInfo = _methodInfo.ApplySubstitution(_fixedMap);

                var assocsB = ImmutableHashSet.CreateBuilder<TypeParameterSymbol>();
                foreach (var associatedParam in _associatedTypes)
                {
                    if (_fixedMap.SubstituteType(associatedParam).AsTypeSymbolOnly() == associatedParam)
                    {
                        assocsB.Add(associatedParam);
                    }
                }
                _associatedTypes = assocsB.ToImmutable();
            }

            /// <summary>
            /// Try inferring a single witness parameter.
            /// </summary>
            /// <param name="conceptWitness">
            /// The witness parameter to infer.
            /// </param>
            /// <param name="useSiteDiagnostics">
            /// The use-site diagnostics set to emit diagnostics to.
            /// </param>
            /// <returns>
            /// True if inference succeeded.
            /// Otherwise, false.
            /// </returns>
            private bool InferWitness(TypeParameterSymbol conceptWitness, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
            {
                var requiredConcepts = GetRequiredConceptsFor(conceptWitness, _fixedMap);
                var candidate = _parent.InferWitnessSatisfyingConcepts(requiredConcepts, _fixedMap, _methodInfo, _chain);

                if (!candidate.Viable)
                {
                    // TODO(@MattWindsor91): diag. handling is likely wrong.
                    var diags = candidate.Diagnostics.AsImmutableOrNull();
                    if (useSiteDiagnostics != null && !diags.IsDefaultOrEmpty)
                    {
                        for (int j = 0; j < diags.Length; j++)
                        {
                            useSiteDiagnostics.Add(diags[j]);
                        }
                    }

                    return false;
                }

                // This concept has now been inferred, so we do two things.
                Debug.Assert(candidate.Instance.IsInstanceType() || candidate.Instance.IsConceptWitness,
                    "Concept witness inference returned something other than a concept instance or witness");

                // 1) Add the inferred concept itself into the substitution.
                //    This has to happen before we compose the unification,
                //    to avoid accidentally clobbering the witness's
                //    type parameter with any other like-named parameters
                //    coming from recursive calls.
                //
                //    TODO: why?
                _fixedMap = _fixedMap.Add(conceptWitness, new TypeWithModifiers(candidate.Instance));
                // 2) Add its unification into our substitution, which will
                //    propagate any associated types fixed by the concept.
                //
                //    This is a sequential composition, so order matters.
                //    We must make sure that, if there are clashes on type
                //    parameters, the outermost assignment wins.
                _fixedMap = _fixedMap.RestrictedCompose(candidate.Unification, ToFix);

                return true;
            }
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
        /// <param name="chainOpt">
        /// The set of instances we've passed through recursively to get here,
        /// used to abort recursive calls if they will create cycles.
        /// </param>
        /// <returns>
        /// Null if inference failed; else, the inferred concept instance and
        /// its unification.
        /// </returns>
        public Candidate InferWitnessSatisfyingConcepts(
            ImmutableArray<NamedTypeSymbol> requiredConcepts,
            ImmutableTypeMap fixedMap,
            MethodInfo methodInfo,
            ImmutableHashSet<NamedTypeSymbol> chainOpt)
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

            // Stop if we might fix an associated type in an invalid place.
            // TODO(@MattWindsor91): really, associated types should come with
            //     information about the exact witness that defines them.
            if (MatchedAssociatedWithNonAssociated(requiredConcepts))
            {
                return default;
            }

            // This lets us use InferOneWitnessFromRequiredConcepts from
            // outside the main concept inferrer, where we aren't going to have
            // a chain.
            var chain = chainOpt ?? ImmutableHashSet<NamedTypeSymbol>.Empty;

            // An instance satisfies inference if:
            // 1) for all concepts required by the type parameter, at least
            //    one concept implemented by the instances unifies with that
            //    concept without capturing bound type parameters
            //    (witness :- instance);
            var firstPassInstances = AllInstancesSatisfyingGoal(requiredConcepts);
            if (firstPassInstances.IsDefaultOrEmpty)
            {
                return Unsatisfiable(requiredConcepts);
            }
            Debug.Assert(firstPassInstances.Length <= _allInstances.Length,
                "First pass of concept witness inference should not grow the instance list");

            // 2) all of the type parameters of that instance can be bound,
            //    both by the substitutions from the unification above and also
            //    by recursively trying to infer any missing concept witnesses.
            //    (instance :- dependency1; dependency2; ...).
            var secondPassInstances = ToSatisfiableInstances(firstPassInstances, methodInfo, chain);
            if (secondPassInstances.IsDefaultOrEmpty)
            {
                return Unsatisfiable(requiredConcepts);
            }
            Debug.Assert(secondPassInstances.Length <= firstPassInstances.Length,
                "Second pass of concept witness inference should not grow the instance list");

            // If we had more than one candidate after 2), we tie-break.
            if (secondPassInstances.Length == 1)
            {
                Debug.Assert(secondPassInstances[0].Instance != null,
                    "Inference claims to have succeeded, but has returned a null instance");
                return secondPassInstances[0];
            }
            var thirdPassInstances = TieBreakInstances(secondPassInstances);
            Debug.Assert(thirdPassInstances.Length <= secondPassInstances.Length,
                "Third pass of concept witness inference should not grow the instance list");
            Debug.Assert(!thirdPassInstances.IsDefaultOrEmpty,
                "Third pass of concept witness inference should only break ties");
            if (thirdPassInstances.Length != 1)
            {
                return Ambiguous(requiredConcepts, thirdPassInstances);
            }
            Debug.Assert(thirdPassInstances[0].Instance != null,
                "Inference claims to have succeeded, but has returned a null instance");
            return thirdPassInstances[0];
        }

        /// <summary>
        /// Checks whether a set of required concepts has an unfixed associated
        /// type parameter in a non-associated position.
        /// </summary>
        /// <param name="concepts">The bundle of concepts to check.</param>
        /// <returns>
        /// True if any of the concepts in <paramref name="concepts"/> has an
        /// associated type as the type argument of a non-associated type
        /// parameter.
        /// </returns>
        private bool MatchedAssociatedWithNonAssociated(ImmutableArray<NamedTypeSymbol> concepts)
        {
            foreach (var concept in concepts)
            {
                var arity = concept.Arity;
                if (arity == 0)
                {
                    continue;
                }

                for (var i = 0; i < arity; ++i)
                {
                    var arg = concept.TypeArguments[i];
                    var par = concept.TypeParameters[i];

                    // We can put an associated type in non-associated position
                    // if it's fixed.  Usually this happens if it's inside the
                    // type parameters of the enclosing context.
                    //
                    // For example, the use of B in Concept2 is always valid in
                    //
                    // void Demo<A, [AssociatedType]B, implicit W>(A a)
                    //     where W : Concept1<A, B>
                    // {
                    //     return Concept2<B>.Test1(W.Test2(a));
                    // }
                    if (TypeArgumentIsFixed(arg, _rigidParams))
                    {
                        continue;
                    }

                    if (arg.IsAssociatedType && !par.IsAssociatedType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a failing candidate with an unsatisfiability error.
        /// </summary>
        /// <param name="concepts">The concepts we tried to infer.</param>
        /// <returns>
        /// A non-viable <see cref="Candidate"/> representing unsatisfiability.
        /// </returns>
        private static Candidate Unsatisfiable(ImmutableArray<NamedTypeSymbol> concepts)
        {
            var conceptDisplay = concepts.Length == 1 ? concepts[0].ToDisplayString() : "(multiple concepts)";
            // CS8957: No instances in scope satisfy concept '0'.
            var err = new CSDiagnosticInfo(ErrorCode.ERR_ConceptInstanceUnsatisfiable, conceptDisplay);
            var errs = new HashSet<DiagnosticInfo> { err };
            return new Candidate(errs);
        }

        /// <summary>
        /// Creates a failing candidate with an ambiguity error.
        /// </summary>
        /// <param name="concepts">The concepts we tried to infer.</param>
        /// <param name="instances">The set of ambiguous instances.</param>
        /// <returns>
        /// A non-viable <see cref="Candidate"/> representing ambiguity.
        /// </returns>
        private static Candidate Ambiguous(ImmutableArray<NamedTypeSymbol> concepts, ImmutableArray<Candidate> instances)
        {
            var conceptDisplay = concepts.Length == 1 ? concepts[0].ToDisplayString() : "(multiple concepts)";
            // CS8958: Cannot infer a unique instance for concept '{0}'. For example, both '{1}' and '{2}' are valid instances.
            var err =
                new CSDiagnosticInfo(
                    ErrorCode.ERR_ConceptInstanceAmbiguous,
                    conceptDisplay,
                    instances[0].Instance.ToDisplayString(),
                    instances[1].Instance.ToDisplayString());
            var errs = new HashSet<DiagnosticInfo> { err };
            return new Candidate(errs);
        }

        /// <summary>
        /// Deduces the set of concepts that must be implemented by any witness
        /// supplied to the given type parameter.
        /// </summary>
        /// <param name="typeParam">The type parameter being inferred.</param>
        /// <param name="fixedMap">
        /// A map mapping fixed type parameters to their type arguments.
        /// </param>
        /// <returns>
        /// An array of concepts required by <paramref name="typeParam"/>.
        /// </returns>
        private static ImmutableArray<NamedTypeSymbol> GetRequiredConceptsFor(TypeParameterSymbol typeParam, ImmutableTypeMap fixedMap)
        {
            var rawRequiredConcepts = typeParam.ProvidedConcepts;

            // The concepts from above are in terms of the method's type
            // parameters.  In order to be able to unify properly, we need to
            // substitute the inferences we've made so far.
            var rc = new ArrayBuilder<NamedTypeSymbol>();
            foreach (var con in rawRequiredConcepts)
            {
                rc.Add(fixedMap.SubstituteNamedType(con));
            }

            var unused = new HashSet<DiagnosticInfo>();

            // Now we can do some optimisation: if we're asking for a concept,
            // we don't need to ask for its base concepts.
            // This is analogous to Haskell context reduction, but somewhat
            // simpler: because of the way our concepts are architected, much
            // of what Haskell does makes no sense.
            var rc2 = new ArrayBuilder<NamedTypeSymbol>();
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
        private ImmutableArray<Candidate> AllInstancesSatisfyingGoal(ImmutableArray<NamedTypeSymbol> requiredConcepts)
        {
            Debug.Assert(!requiredConcepts.IsEmpty,
                "First pass of inference is pointless when there are no required concepts");
            Debug.Assert(!_allInstances.IsEmpty,
                "First pass of inference is pointless when there are no available instances");

            // First, collect all of the instances satisfying 1).
            var firstPassInstanceBuilder = new ArrayBuilder<Candidate>();
            foreach (var instance in _allInstances)
            {
                // @MattWindsor91 (Concept-C# 2017)
                // We use type unification here, which is sound but incomplete
                // (doesn't respect variance and subtyping).
                //
                // The correct thing to do here would be to use something like
                // MethodTypeInference's LowerBoundInterfaceInference, with
                // care to make sure any associated types and witnesses are
                // recursively dispatched as in the second pass.
                if (AllRequiredConceptsProvided(requiredConcepts, instance, out ImmutableTypeMap unifyingSubstitutions, _rigidParams))
                {
                    // The unification may provide us with substitutions we
                    // need to make provided concepts match required concepts.
                    // Some of these substitutions may also need to
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
        /// <param name="instance">The candidate instance.</param>
        /// <param name="unifyingSubstitutions">
        /// A map of type substitutions, populated by this method, which are
        /// required in order to make the instance implement the concepts.
        /// </param>
        /// <returns>
        /// True if, and only if, the given instance implements the given list
        /// of concepts.
        /// </returns>
        private bool AllRequiredConceptsProvided(ImmutableArray<NamedTypeSymbol> requiredConcepts, TypeSymbol instance, out ImmutableTypeMap unifyingSubstitutions, ImmutableHashSet<TypeParameterSymbol> boundAndUnfixedParams)
        {
            Debug.Assert(!requiredConcepts.IsEmpty,
                "Checking that all required concepts are provided is pointless when there are none");

            var subst = new MutableTypeMap();
            unifyingSubstitutions = new ImmutableTypeMap();

            var providedConcepts =
                ((instance as TypeParameterSymbol)?.AllEffectiveInterfacesNoUseSiteDiagnostics
                    ?? ((instance as NamedTypeSymbol)?.AllInterfacesNoUseSiteDiagnostics)
                    ?? ImmutableArray<NamedTypeSymbol>.Empty);
            if (providedConcepts.IsEmpty)
            {
                return false;
            }

            foreach (var requiredConcept in requiredConcepts)
            {
                if (!IsRequiredConceptProvided(requiredConcept, providedConcepts, ref subst, boundAndUnfixedParams))
                {
                    return false;
                }
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
        private bool IsRequiredConceptProvided(TypeSymbol requiredConcept, ImmutableArray<NamedTypeSymbol> providedConcepts, ref MutableTypeMap unifyingSubstitutions, ImmutableHashSet<TypeParameterSymbol> boundAndUnfixedParams)
        {
            Debug.Assert(!providedConcepts.IsEmpty,
                "Checking for provision of concept is pointless when no concepts are provided");

            foreach (var providedConcept in providedConcepts)
            {
                if (TypeUnification.CanUnify(requiredConcept, providedConcept, ref unifyingSubstitutions, boundAndUnfixedParams))
                {
                    return true;
                }
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
            MethodInfo methodInfo,
            ImmutableHashSet<NamedTypeSymbol> chain)
        {
            // Remember: even if we have one instance left here, it could be
            // unsatisfiable, so we have to run this pass on it.
            Debug.Assert(!candidateInstances.IsEmpty,
                "Performing second pass of witness inference is pointless when we have no candidates left");

            var secondPassInstanceBuilder = new ArrayBuilder<Candidate>();
            foreach (var candidate in candidateInstances)
            {
                Candidate fixedInstance = InferRecursively(candidate, methodInfo, chain);
                if (fixedInstance.Viable)
                {
                    secondPassInstanceBuilder.Add(fixedInstance);
                }
            }
            return secondPassInstanceBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Prepares a recursive inference round on an instance.
        /// </summary>
        /// <param name="candidate">
        /// The candidate instance that needs recursive inference.
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
            Candidate candidate,
            MethodInfo methodInfo,
            ImmutableHashSet<NamedTypeSymbol> chain)
        {
            // Early out: if the instance is a type param, we need not recur.
            if (candidate.Instance.Kind == SymbolKind.TypeParameter)
            {
                return candidate;
            }

            Debug.Assert(candidate.Instance.Kind == SymbolKind.NamedType,
                "If an instance is not a parameter, it should be a named type.");

            var nt = (NamedTypeSymbol)candidate.Instance;

            // Early out: if we have no type parameters, we need not recur. 
            if (nt.TypeParameters.IsEmpty)
            {
                return candidate;
            }

            // Do cycle detection: have we already set up a recursive
            // call for this instance with these type parameters?
            if (chain.Contains(nt))
            {
                return default;
            }

            // We need to pass the old fixed map into the new recursion.
            // Recursive inference using associated types seemingly
            // can't be driven entirely from the fixed parameters of the
            // candidate instance.
            //
            // TODO: understand exactly why this is.
            // TODO: maybe we only need to keep the fixed map around, not
            //       the whole unification?
            var newMethodInfo = methodInfo.ApplySubstitution(candidate.Unification);
            var round = NewRound(nt.TypeParameters, nt.TypeArguments, candidate.Unification, newMethodInfo, chain.Add(nt));
            var diags = new HashSet<DiagnosticInfo>();
            var result = round.Infer(ref diags);
            if (!result.Success)
            {
                // We don't care if we failed to fix method type parameters.
                // They might be fixable by other instances.
                // TODO: consistency ramifications of the above.
                // NB: diags might be empty at this stage (TODO: should it be?)
                return new Candidate(diags);
            }

            var fixedInstance = result.Map.SubstituteNamedType(nt);
            // Finally, check constraints on the fully fixed instance.
            // We do this here in case the constraint satisfaction depends
            // on the recursive inference.
            if (!fixedInstance.CheckAllConstraints(_conversions))
            {
                return default;
            }

            return new Candidate(fixedInstance, result.Map);
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
            // TODO(@MattWindsor91): better tie-breaking.
            // TODO(@MattWindsor91): align with overload resolution rules.
            // TODO(@MattWindsor91): formally specify this--it is quite ad-hoc.

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
            // Sometimes, the above can eliminate _all_ instances.
            // In this case, we want to return the original set, since
            // no winners is the same as everyone winning.
            //
            // TODO(@MattWindsor91): stop the elimination?
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
            // TODO(@MattWindsor91): type parameters?

            // Overlap is permitted if:
            //     1) the overlapping type is marked [Overlapping]; or
            var r = overlapper.Instance;
            var rOverlapping = r.Kind == SymbolKind.NamedType && ((NamedTypeSymbol)r).IsOverlapping;
            if (rOverlapping)
            {
                return true;
            }
            //     2) the overlapped type is marked [Overlappable].
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
        /// <param name="moreSpecific">The instance to compare against.</param>
        /// <param name="lessSpecific">The instance to compare.</param>
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
            var conceptParamsBuilder = ImmutableHashSet.CreateBuilder<TypeParameterSymbol>();
            var associatedParamsBuilder = ImmutableHashSet.CreateBuilder<TypeParameterSymbol>();
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
            var conceptWitnesses = conceptParamsBuilder.ToImmutable();

            var round = new NormalInferRound(
                parent: this,
                conceptWitnesses: conceptWitnesses,
                associatedTypes: associatedParamsBuilder.ToImmutable(),
                fixedMap: fixedMap.ToUnification(),
                methodInfo: new AbsentMethodInfo(),
                chain: ImmutableHashSet<NamedTypeSymbol>.Empty);

            // TODO: pass in diagnostics
            var ignore = new HashSet<DiagnosticInfo>();
            var result = round.Infer(ref ignore);
            var unification = result.Map;
            if (!result.Success)
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
                if (!(expandAssociatedIfFailed && conceptWitnesses.IsEmpty))
                {
                    return ImmutableArray<TypeSymbol>.Empty;
                }

                // This will just cause each unfixed type parameter to bubble
                // through in the next section.
                unification = new ImmutableTypeMap();
            }

            // As in normal inference, it is not necessarily a bug for
            // the map to map typeParameters[i] to typeParameters[i].
            // This might be, eg., a recursive call.
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
