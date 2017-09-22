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
        internal enum ConceptInstanceSearchOptions
        {
            /// <summary>
            /// Default behaviour.
            /// The default behaviour is useless, and will need OR-ing with
            /// other flags for search to work.
            /// </summary>
            Default = 0,
            /// <summary>
            /// Only consider witnesses directly brought in scope by a type
            /// parameter.
            /// </summary>
            OnlyExplicitWitnesses = 1 << 0,
            /// <summary>
            /// Search in containers.
            /// </summary>
            SearchContainers = 1 << 1,
            /// <summary>
            /// If the current scope has any 'using' statements, search through
            /// those too.
            /// </summary>
            SearchUsings = 1 << 2
        }

        /// <summary>
        /// Retrieves the list of witnesses available in this particular
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
        internal virtual void GetConceptInstances(ConceptInstanceSearchOptions options, ArrayBuilder<TypeSymbol> instances, Binder originalBinder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // By default, binders have no instances.
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

        internal virtual void LookupConceptMethodsInSingleBinder(LookupResult result, string name, int arity, ConsList<Symbol> basesBeingResolved, LookupOptions options, Binder originalBinder, bool diagnose, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var instanceBuilder = ArrayBuilder<TypeSymbol>.GetInstance();
            GetConceptInstances(ConceptInstanceSearchOptions.OnlyExplicitWitnesses | ConceptInstanceSearchOptions.SearchContainers | ConceptInstanceSearchOptions.SearchUsings, instanceBuilder, originalBinder, ref useSiteDiagnostics);
            var instances = instanceBuilder.ToImmutableAndFree();
            foreach (var instance in instances)
            {
                // Currently only explicit witnesses, ie type parameters, may
                // be probed for concept methods.
                var tpInstance = instance as TypeParameterSymbol;
                if (tpInstance == null) continue;
                LookupSymbolsInWitness(tpInstance, result, name, arity, basesBeingResolved, options, originalBinder, diagnose, ref useSiteDiagnostics);
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
                        // We don't allow other types to be fields of a
                        // witness.
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

            if (!namedType.IsConcept) return false;

            for (int i = 0; i < namedType.TypeParameters.Length; i++)
            {
                if (namedType.TypeParameters[i] == namedType.TypeArguments[i]) return true;
            }

            // We didn't see any evidence of failed inference at this point.
            return false;
        }
    }
}
