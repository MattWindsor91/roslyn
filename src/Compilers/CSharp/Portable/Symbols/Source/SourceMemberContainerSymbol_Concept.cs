// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // @MattWindsor91 (Concept-C# 2017)
    // New members for doing concept-specific work.

    internal partial class SourceMemberContainerTypeSymbol
    {
        #region Concept and instance selectors

        /// <summary>
        /// Gets whether this symbol represents a concept.
        /// </summary>
        /// <returns>
        /// True if this symbol is a concept (either it was declared as a
        /// concept, or it is an interface with the <c>System_Concepts_ConceptAttribute</c>
        /// attribute); false otherwise.
        /// </returns>
        internal override bool IsConcept => 
            MergedDeclaration.Kind == DeclarationKind.Concept ||
            (IsInterface && HasConceptAttribute);

        /// <summary>
        /// Gets whether this symbol represents a concept instance.
        /// </summary>
        /// <returns>
        /// True if this symbol was declared as an instance; false otherwise.
        /// </returns>
        internal override bool IsInstance =>
            MergedDeclaration.Kind == DeclarationKind.Instance;
        // This used to check HasInstanceAttribute, but this leads to infinite
        // loops.

        #endregion Concept and instance selectors

        #region Default method detection

        /// <summary>
        /// Syntax references for default methods.
        /// </summary>
        private ImmutableArray<SyntaxReference> _conceptDefaultMethods;

        /// <summary>
        /// Get syntax references for all of the default method implementations
        /// on this symbol, if it is a concept.
        /// </summary>
        /// <returns>
        /// An array of method syntax references.
        /// </returns>
        internal ImmutableArray<SyntaxReference> GetConceptDefaultMethods()
        {
            // TODO(@MattWindsor91): Find a better way to make sure this is
            //     populated.
            if (_conceptDefaultMethods.IsDefault) GetMembers();
            Debug.Assert(!_conceptDefaultMethods.IsDefault, "concept default methods should be populated at this stage.");
            return _conceptDefaultMethods;
        }

        #endregion Default method detection

        #region Shim synthesis

        // Instance shims are methods that fill in a gap in a concept instance
        // by forwarding out to some other implementation of the method.
        // This region contains the synthesis code for generating them.
        //
        // TODO(@MattWindsor91): We hijack the explicit implementation
        //     forwarding logic to insert shims, which works but is inelegant.
        //
        // TODO(@MattWindsor91): Can we use binders to work out if a shim is
        //     viable? I don't think we can, but it would be much more robust.

        /// <summary>
        /// Lazy-loaded backing for synthesized instance shims.
        /// </summary>
        private ImmutableArray<SynthesizedInstanceShimMethod> _lazySynthesizedInstanceShims;

        /// <summary>
        /// Gets the list of synthesized shims for this
        /// instance.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token for stopping this search abruptly.
        /// </param>
        /// <returns>
        /// The list of synthesized instance shims.
        /// </returns>
        internal ImmutableArray<SynthesizedInstanceShimMethod> GetSynthesizedInstanceShims(
            CancellationToken cancellationToken)
        {
            if (_lazySynthesizedInstanceShims.IsDefault)
            {
                var builder = ArrayBuilder<SynthesizedInstanceShimMethod>.GetInstance();
                var all = GetSynthesizedImplementations(cancellationToken);
                foreach (var impl in all)
                {
                    // TODO(@MattWindsor91): make this not a type switch?
                    if (impl is SynthesizedInstanceShimMethod d)
                    {
                        builder.Add(d);
                    }

                }

                if (ImmutableInterlocked.InterlockedCompareExchange(
                        ref _lazySynthesizedInstanceShims,
                        builder.ToImmutableAndFree(),
                        default).IsDefault)
                {
                    // TODO(@MattWindsor91): do something here?
                }
            }

            return _lazySynthesizedInstanceShims;
        }

        /// <summary>
        /// Tries to synthesize a shim to fill in a given concept method for
        /// this instance.
        /// </summary>
        /// <param name="concept">
        /// The concept containing the method to be shimmed.
        /// </param>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <param name="diagnostics">
        /// A diagnostics set, to which we report default struct failures.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim;
        /// otherwise, the created shim.
        /// </returns>
        private SynthesizedInstanceShimMethod TrySynthesizeInstanceShim(NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            Debug.Assert(concept.IsConcept, "concept for default implementation synthesis must be an actual concept");
            Debug.Assert(IsInstance, "target for default implementation synthesis must be an instance");

            if (conceptMethod.IsOperator())
            {
                var ometh = TrySynthesizeOperatorShim(concept, conceptMethod, diagnostics);
                if (ometh != null)
                {
                    return ometh;
                }
            }

            if (conceptMethod.IsConceptExtensionMethod)
            {
                var emeth = TrySynthesizeConceptExtensionShim(concept, conceptMethod, diagnostics);
                if (emeth != null)
                {
                    return emeth;
                }
            }

            // Intentionally synthesize defaults as a last resort.
            var dstr = concept.GetDefaultStruct();
            if (dstr != null)
            {
                var dmeth = TrySynthesizeDefaultShim(dstr, concept, conceptMethod, diagnostics);
                if (dmeth != null)
                {
                    return dmeth;
                }
            }

            return null;
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept operator
        /// to a static operator on the operator's first parameter type.
        /// </summary>
        /// <param name="concept">
        /// The concept containing the default struct.
        /// </param>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <param name="diagnostics">
        /// A diagnostics set, to which we report default struct failures.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// </returns>
        private SynthesizedOperatorShimMethod TrySynthesizeOperatorShim(NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            // TODO(MattWindsor91): consider opening this up, with
            //     restrictions, to any static method.
            Debug.Assert(conceptMethod.IsOperator(),
                "shouldn't synthesize an operator shim on a non-operator");
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "concept operator should have at least one parameter");

            var target = conceptMethod.ParameterTypes[0];
            var shim = new SynthesizedOperatorShimMethod(conceptMethod, this);
            return shim.IsValid() ? shim : null;
        }
        
        /// <summary>
        /// Tries to generate an instance shim mapping a concept extension method
        /// to an actual method on its 'this' parameter's type.
        /// </summary>
        /// <param name="concept">
        /// The concept containing the default struct.
        /// </param>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <param name="diagnostics">
        /// A diagnostics set, to which we report default struct failures.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// </returns>
        private SynthesizedConceptExtensionShimMethod TrySynthesizeConceptExtensionShim(NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            Debug.Assert(conceptMethod.IsConceptExtensionMethod,
                "shouldn't try synthesising this on a static concept method");
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "concept extension method should have a 'this' parameter");

            var target = conceptMethod.ParameterTypes[0];
            ImmutableArray<Symbol> members;
            if (target.Kind == SymbolKind.NamedType)
            {
                var nt = (NamedTypeSymbol)target;
                members = nt.GetSimpleNonTypeMembers(conceptMethod.Name);
            }
            else
            {
                members = target.GetMembers(conceptMethod.Name);
            }

            foreach (var member in members)
            {
                if (member.Kind == SymbolKind.Method
                    && CanMakeConceptExtensionShim(from: conceptMethod, to: (MethodSymbol)member))
                {
                    return new SynthesizedConceptExtensionShimMethod(conceptMethod, this);
                }
            }

            return null;
        }

        /// <summary>
        /// Can we make a concept extension shim between these methods?
        /// </summary>
        /// <param name="from">
        /// The concept method we're trying to implement with a shim.
        /// </param>
        /// <param name="to">
        /// The method we're trying to call into with a shim.
        /// </param>
        /// <returns>
        /// Whether it would be valid for a concept extension shim to implement
        /// <paramref name="from"/> by calling into
        /// <paramref name="to"/>.
        /// </returns>
        private static bool CanMakeConceptExtensionShim(MethodSymbol from, MethodSymbol to)
        {
            // TODO(@MattWindsor91): these checks are flaky, and likely
            //     incomplete.
            if (to.IsStatic)
            {
                return false;
            }
            if (from.Arity != to.Arity)
            {
                return false;
            }
            // TODO(@MattWindsor91): is this too strict?
            if (to.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }
            // Remember: `to` has an implicit 'this', `from` does not.
            if (from.ParameterCount - 1 != to.ParameterCount)
            {
                return false;
            }

            // To compare, we must rewrite `from` from
            // B(A, C, D) to A.B(C, D).
            var extParamsB = ArrayBuilder<ParameterSymbol>.GetInstance();
            extParamsB.AddRange(from.Parameters.Skip(1));
            var extParams = extParamsB.ToImmutableAndFree();
            var shuffledFrom = new SignatureOnlyMethodSymbol(
                from.Name,
                from.ContainingType,
                from.MethodKind,
                from.CallingConvention,
                from.TypeParameters,
                extParams,
                from.RefKind,
                from.ReturnType,
                from.ReturnTypeCustomModifiers,
                from.RefCustomModifiers,
                from.ExplicitInterfaceImplementations);
            return MemberSignatureComparer.CSharpImplicitImplementationComparer.Equals(
                shuffledFrom, to);
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept method to
        /// one on the given default struct.
        /// </summary>
        /// <param name="dstr">
        /// The default struct into which the shim will call.
        /// </param>
        /// <param name="concept">
        /// The concept containing the default struct.
        /// </param>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <param name="diagnostics">
        /// A diagnostics set, to which we report default struct failures.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// </returns>
        private SynthesizedDefaultShimMethod TrySynthesizeDefaultShim(NamedTypeSymbol dstr, NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            // Default-struct sanity checking
            var conceptLoc = concept.Locations.IsEmpty ? Location.None : Locations[0];
            var instanceLoc = Locations.IsEmpty ? Location.None : Locations[0];
            if (dstr.Arity != 1)
            {
                // Don't use the default struct's location: it is an
                // implementation detail and may not actually exist.
                diagnostics.Add(ErrorCode.ERR_DefaultStructBadArity, conceptLoc, concept.Name, dstr.Arity, concept.Arity + 1);
                return null;
            }
            var witnessPar = dstr.TypeParameters[0];
            if (!witnessPar.IsConceptWitness)
            {
                diagnostics.Add(ErrorCode.ERR_DefaultStructNoWitnessParam, conceptLoc, concept.Name);
                return null;
            }

            // Check that the defaults struct actually contains this method
            // TODO(MattWindsor91): check this works for properties
            var eco = MemberSignatureComparer.CSharpImplicitImplementationComparer;
            foreach (var member in dstr.GetMembersUnordered())
            {
                if (member.Kind != SymbolKind.Method)
                {
                    continue;
                }
                if (!eco.Equals(conceptMethod, member))
                {
                    continue;
                }

                return new SynthesizedDefaultShimMethod(conceptMethod, dstr, this);
            }

            return null;
        }

        #endregion Shim synthesis
    }
}
