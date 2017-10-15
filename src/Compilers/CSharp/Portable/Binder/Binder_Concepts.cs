// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;
using Roslyn.Utilities;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// Bitfield of search options when getting concept instances.
        /// </summary>
        internal enum ConceptSearchOptions
        {
            /// <summary>
            /// If looking for concepts, accept standalone instances too.
            /// </summary>
            AllowStandaloneInstances = 1 << 0,
            /// <summary>
            /// Search in containers.
            /// </summary>
            SearchContainers = 1 << 1,
            /// <summary>
            /// If the current scope has any 'using' statements, search through
            /// those too.
            /// </summary>
            SearchUsings = 1 << 2,
            /// <summary>
            /// Consider concept extension methods only.
            /// </summary>
            ConceptExtensionsOnly = 1 << 3,
            /// <summary>
            /// Consider non-extension methods only.
            /// </summary>
            NoConceptExtensions = 1 << 4,
        }

        /// <summary>
        /// Retrieves the list of concept instances available in this
        /// binder's scope.
        /// </summary>
        /// <param name="options">
        /// The search options to use when retrieving the list.
        /// </param>
        /// <param name="instances">
        /// The array builder to populate with instances.
        /// </param>
        /// <param name="originalBinder">
        /// The call-site binder.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// Diagnostics set at the use-site.
        /// </param>
        internal virtual void GetConceptInstances(ConceptSearchOptions options, ArrayBuilder<TypeSymbol> instances, Binder originalBinder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // By default, binders have no instances.
        }

        /// <summary>
        /// Retrieves the list of concepts available in this
        /// binder's scope.
        /// </summary>
        /// <param name="options">
        /// The search options to use when retrieving the list.
        /// </param>
        /// <param name="concepts">
        /// The array builder to populate with concepts.
        /// </param>
        /// <param name="originalBinder">
        /// The call-site binder.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// Diagnostics set at the use-site.
        /// </param>
        internal virtual void GetConcepts(ConceptSearchOptions options, ArrayBuilder<NamedTypeSymbol> concepts, Binder originalBinder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // By default, binders have no concepts.
        }

        /// <summary>
        /// Retrieves the type parameters fixed by parameter lists in
        /// this particular binder's scope.
        /// </summary>
        /// <param name="fixedTypeParams">
        /// The array builder to populate with type parameters.
        /// </param>
        internal virtual void GetFixedTypeParameters(ArrayBuilder<TypeParameterSymbol> fixedTypeParams)
        {
            // By default, binders have no fixed type parameters.
            return;
        }

        internal virtual void LookupConceptMethodsInSingleBinder(LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics, ConceptSearchOptions coptions)
        {
            var conceptBuilder = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            GetConcepts(coptions, conceptBuilder, originalBinder, ref useSiteDiagnostics);
            var concepts = conceptBuilder.ToImmutableAndFree();

            foreach (var concept in concepts)
            {
                var methodBuilder = ArrayBuilder<MethodSymbol>.GetInstance();
                AddConceptMethods(concept, methodBuilder, name, arity, options, coptions);
                foreach (var method in methodBuilder.ToImmutableAndFree())
                {
                    SingleLookupResult resultOfThisMethod = originalBinder.CheckViability(method, arity, options, concept, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    result.MergeEqual(resultOfThisMethod);
                }
            }
        }

        /// <summary>
        /// Tries to look up symbols inside a witness type parameter.
        /// <para>
        /// This lookup checks all of the concepts this witness implements
        /// to see if any contain a viable method matching the symbol.
        /// </para>
        /// <para>
        /// This lookup approach only works for methods and properties, and
        /// returns members whose parents are type parameters.  We rely on
        /// later stages to detect this and resolve it back to a proper
        /// statement.
        /// </para>
        /// </summary>
        /// <param name="witness">
        /// The type witness into which we are looking.
        /// </param>
        /// <param name="result">
        /// The lookup result to populate.
        /// </param>
        /// <param name="name">
        /// The name of the member being looked-up.
        /// </param>
        /// <param name="arity">
        /// The arity of the member being looked up.
        /// </param>
        /// <param name="basesBeingResolved">
        /// The set of bases being resolved.
        /// </param>
        /// <param name="options">
        /// The lookup options in effect.
        /// </param>
        /// <param name="originalBinder">
        /// The top-level binder.
        /// </param>
        /// <param name="diagnose">
        /// Whether or not we are diagnosing.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// Diagnostics set at the use-site.
        /// </param>
        internal void LookupSymbolsInWitness(
            TypeParameterSymbol witness, LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            Debug.Assert(witness.IsConceptWitness);

            var concepts = witness.ProvidedConcepts;
            if (concepts.IsDefaultOrEmpty)
            {
                return;
            }
            foreach (var c in concepts)
            {
                var members = GetCandidateMembers(c, name, options, originalBinder);
                foreach (var member in members)
                {
                    switch (member.Kind)
                    {
                        case SymbolKind.Method:
                            var method = (MethodSymbol)member;
                            // Suppose our witness is W : C<A>, and this finds C<A>.M(x).
                            // We need to return that we found W.M(x), but W is a type
                            // parameter!  While we can handle this later on in binding,
                            // the main issue is changing C<A> to W, for which we use
                            // a synthesized method symbol.
                            var witnessMethod = new SynthesizedWitnessMethodSymbol(method, witness);
                            SingleLookupResult resultOfThisMethod = originalBinder.CheckViability(witnessMethod, arity, options, witness, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                            result.MergeEqual(resultOfThisMethod);
                            break;
                        case SymbolKind.Property:
                            var prop = (PropertySymbol)member;
                            var witnessProp = new SynthesizedWitnessPropertySymbol(prop, witness);
                            SingleLookupResult resultOfThisProp = originalBinder.CheckViability(witnessProp, arity, options, witness, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                            result.MergeEqual(resultOfThisProp);
                            break;
                        // We don't allow other types to be fields of a witness
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether this symbol is a concept whose part-inference
        /// has failed.
        /// <para>
        /// This guards against the unsound interactions of the feature
        /// allowing part-inference to return a 'not quite inferred' concept
        /// in the situations where the missing type parameters are associated
        /// types that will be filled in when an instance of that concept is
        /// substituted for its witness.
        /// </para>
        /// </summary>
        /// <param name="namedType">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if this type is a concept, and has at least one unfixed
        /// type parameter.
        /// </returns>
        bool IsConceptWithFailedPartInference(NamedTypeSymbol namedType)
        {
            // @t-mawind
            //   Perhaps the existence of this method is evidence against the
            //   usefulness of allowing concepts on the left hand side of
            //   member accesses?

            if (!namedType.IsConcept)
            {
                return false;
            }

            for (int i = 0; i < namedType.TypeParameters.Length; i++)
            {
                if (namedType.TypeParameters[i] == namedType.TypeArguments[i])
                {
                    return true;
                }
            }

            // We didn't see any evidence of failed inference at this point.
            return false;
        }

        /// <summary>
        /// Search this scope for any directly accessible concept instances
        /// with extension methods, and add any matching Concept Extension
        /// Methods into scope.
        /// </summary>
        /// <param name="searchUsingsNotNamespace">
        /// If true, and we are looking inside a namespace, search its 'using'
        /// imports for CEMs instead of the namespace instead.
        /// </param>
        /// <param name="methods">
        /// The method array being populated.
        /// Any found candidate CEMs are added here.
        /// </param>
        /// <param name="name">
        /// The name of the method under lookup.
        /// </param>
        /// <param name="arity">
        /// The arity of the method under lookup.
        /// </param>
        /// <param name="options">
        /// The option set being used for this lookup.
        /// </param>
        /// <param name="originalBinder">
        /// The binder at the scope of the original method invocation.
        /// </param>
        internal void GetCandidateConceptExtensionMethods(
            bool searchUsingsNotNamespace,
            ArrayBuilder<MethodSymbol> methods,
            string name,
            int arity,
            LookupOptions options,
            Binder originalBinder)
        {
            // @MattWindsor91 (Concept-C# 2017)
            //
            // We find CEMs by searching for all concepts in scope,
            // picking any methods in those concepts that are
            // candidates, and posing the search for an instance as a
            // method type inference problem.
            //
            // This works, but has two problems:
            //
            // 1) Performance: we have to consider concepts that don't even
            //    have scoped instances, let alone ones that don't have
            //    viable ones.  We also have to do a run of concept inference
            //    even if we already have a witness in the form of a type
            //    parameter.
            // 2) Integrity: the rewriting of the instance search as a MTI
            //    problem needs us to mangle the method symbol heavily to make
            //    it pass through to MTI in the first place, and de-mangle it
            //    at the other end.
            //
            // If concept inference and overload resolution were perfectly
            // aligned (eg. overload resolution will make the same decision on
            // which method from which instance to use as we would make by
            // picking the method and using concept inference), and there was a
            // decent way to infer missing parameters in instances at this
            // point, we could save a lot of time and compiler arm-twisting by
            // letting the overload resolver work out these issues.

            var coptions = searchUsingsNotNamespace ? ConceptSearchOptions.SearchUsings : ConceptSearchOptions.SearchContainers;
            coptions |= ConceptSearchOptions.ConceptExtensionsOnly;
            // Standalone instances are also treated as concepts for the
            // purposes of CEM resolution.
            coptions |= ConceptSearchOptions.AllowStandaloneInstances;

            var concepts = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            HashSet<DiagnosticInfo> ignore = null;
            GetConcepts(coptions, concepts, originalBinder, ref ignore);

            foreach (var concept in concepts.ToImmutableAndFree())
            {
                AddConceptMethods(concept, methods, name, arity, options, coptions);
            }
        }

        /// <summary>
        /// Adds the concept extension methods available in a concept to a
        /// candidate method list.
        /// </summary>
        /// <param name="concept">
        /// The concept we are searching for CEMs.
        /// </param>
        /// <param name="methods">
        /// The method array being populated.
        /// Any found candidate CEMs are added here.
        /// </param>
        /// <param name="nameOpt">
        /// The name of the method under lookup; may be null.
        /// </param>
        /// <param name="arity">
        /// The arity of the method under lookup.
        /// </param>
        /// <param name="options">
        /// The option set being used for this lookup.
        /// </param>
        /// <param name="conceptOptions">
        /// The concept-level options set being used for this lookup.
        /// </param>
        private void AddConceptMethods(NamedTypeSymbol concept, ArrayBuilder<MethodSymbol> methods, string nameOpt, int arity, LookupOptions options, ConceptSearchOptions conceptOptions)
        {
            var allowExtensions = (conceptOptions & ConceptSearchOptions.NoConceptExtensions) == 0;
            var allowNonExtensions = (conceptOptions & ConceptSearchOptions.ConceptExtensionsOnly) == 0;
            var allowStandaloneInstances = (conceptOptions & ConceptSearchOptions.AllowStandaloneInstances) != 0;

            Debug.Assert(concept != null, "cannot get methods from null concept");
            Debug.Assert(concept.IsConcept || concept.IsStandaloneInstance,
                $"'{nameof(concept)}' is not a concept or standalone instance");
            Debug.Assert(!concept.IsStandaloneInstance || allowStandaloneInstances,
                $"'{nameof(concept)}' is a standalone instance, which is not allowed here");
            Debug.Assert(0 <= arity, "arity cannot be negative");

            // This part is mostly copied from DoGetExtensionMethods.
            var members = nameOpt == null ? concept.GetMembersUnordered() : concept.GetSimpleNonTypeMembers(nameOpt);
            foreach (var member in members)
            {
                if (member.Kind != SymbolKind.Method)
                {
                    continue;
                }
                var method = (MethodSymbol)member;

                var extensionSituationOk = method.IsConceptExtensionMethod ? allowExtensions : allowNonExtensions;
                if (!extensionSituationOk)
                {
                    continue;
                }

                var arityOk = (options & LookupOptions.AllMethodsOnArityZero) != 0 || arity == method.Arity;
                if (!arityOk)
                {
                    continue;
                }

                // @MattWindsor91 (Concept-C# 2017)
                // If we picked up this method from a concept, we need to
                // infer the specific instance we'll actually be calling.
                // Also, if the concept or standalone instance had type
                // parameters, these need to be inferred.
                var mustInferConceptParams = concept.IsConcept || concept.IsGenericType;

                // In these cases, `method` will look like
                //     `C<TC>.M<TM>(this TC x, ...)`.
                // Our prototype solution is to use a synthesised
                // symbol that looks like
                //     `M<TM, TC, implicit I>(this TC x, ...)
                //          where I : C<TC>`
                // which pushes all of the issues into the method
                // type inferrer.  When we construct the method with
                // the inferred arguments, we de-mangle the method back
                // to normal.
                //
                // This is a nice party trick, but should eventually be
                // done properly: see the commentary in
                // `GetCandidateConceptExtensionMethods`.
                var finalMethod = mustInferConceptParams
                    ? (MethodSymbol) new SynthesizedImplicitConceptMethodSymbol(method)
                    : new SynthesizedWitnessMethodSymbol(method, concept);
                methods.Add(finalMethod);
            }
        }
    }
}
