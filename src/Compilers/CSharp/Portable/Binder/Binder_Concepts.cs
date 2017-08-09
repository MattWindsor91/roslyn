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
        /// Retrieves the list of witnesses available in this particular
        /// binder's scope.
        /// </summary>
        /// <param name="onlyExplicitWitnesses">
        /// If true, only return witnesses that have been explicitly put
        /// into scope by a concept constraint.
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
        internal virtual void GetConceptInstances(bool onlyExplicitWitnesses, ArrayBuilder<TypeSymbol> instances, Binder originalBinder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // By default, binders have no instances.
            return;
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
            GetConceptInstances(true, instanceBuilder, originalBinder, ref useSiteDiagnostics);
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
        /// This lookup approach only works for methods, and returns a
        /// method whose parent is a type parameter.  We rely on later
        /// binder stages to detect this and resolve it back to a proper
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

            // Concepts are just interfaces, so we look at every possible
            // interface this witness has been constrained to implement.
            foreach (var iface in witness.AllEffectiveInterfacesNoUseSiteDiagnostics)
            {
                if (!iface.IsConcept) continue;

                // We're assuming that the above handles inheritance for us.
                // This may be a mistake.
                var members = GetCandidateMembers(iface, name, options, originalBinder);
                foreach (var member in members)
                {
                    // Don't bother trying to resolve non-methods:
                    // concepts can't have them, and we only have shims for
                    // dealing with methods later on anyway.
                    if (member.Kind != SymbolKind.Method) continue;
                    var method = member as MethodSymbol;
                    Debug.Assert(method != null);

                    // Suppose our witness is W : C<A>, and this finds C<A>.M(x).
                    // We need to return that we found W.M(x), but W is a type
                    // parameter!  While we can handle this later on in binding,
                    // the main issue is changing C<A> to W, for which we use
                    // a synthesized method symbol.
                    var witnessMethod = new SynthesizedWitnessMethodSymbol(method, witness);
                    SingleLookupResult resultOfThisMember = originalBinder.CheckViability(witnessMethod, arity, options, witness, diagnose, ref useSiteDiagnostics, basesBeingResolved);
                    result.MergeEqual(resultOfThisMember);
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
