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

        #region Default structs

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
            if (_conceptDefaultMethods.IsDefault)
            {
                GetMembersAndInitializers();
                Debug.Assert(!_conceptDefaultMethods.IsDefault,
                    "concept default methods should be populated at this stage.");
            }
            return _conceptDefaultMethods;
        }

        /// <summary>
        /// If this type is a named concept, and it needs one, generate a
        /// default struct.
        /// </summary>
        /// <returns>
        /// Null if this type is not a named type, or it doesn't need a
        /// default struct.
        /// Otherwise, the synthesised default struct.
        /// </returns>
        protected virtual NamedTypeSymbol MaybeMakeDefaultStruct()
        {
            // The actual implementation is in SourceNamedTypeSymbol, but we
            // have a null implementation for other member containers here.
            return null;
        }

        #endregion Default structs

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

            var ometh = TrySynthesizeOperatorShim(conceptMethod);
            if (ometh != null && ometh.IsValid())
            {
                return ometh;
            }
            var emeth = TrySynthesizeConceptExtensionShim(conceptMethod);
            if (emeth != null && emeth.IsValid())
            {
                return emeth;
            }

            // Intentionally synthesize defaults as a last resort.
            var dmeth = TrySynthesizeDefaultShim(concept, conceptMethod, diagnostics);
            if (dmeth != null && dmeth.IsValid())
            {
                return dmeth;
            }

            return null;
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept operator
        /// to a static operator on the operator's first parameter type.
        /// </summary>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// The shim might not be valid: this must be checked before acceptance.
        /// </returns>
        private SynthesizedOperatorShimMethod TrySynthesizeOperatorShim(MethodSymbol conceptMethod)
        {
            // TODO(MattWindsor91): consider opening this up, with
            //     restrictions, to any static method.
            if (!conceptMethod.IsOperator())
            {
                return null;
            }
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "concept operator should have at least one parameter");
            return new SynthesizedOperatorShimMethod(conceptMethod, this);
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept extension method
        /// to an actual method on its 'this' parameter's type.
        /// </summary>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// The shim might not be valid: this must be checked before acceptance.
        /// </returns>
        private SynthesizedConceptExtensionShimMethod TrySynthesizeConceptExtensionShim(MethodSymbol conceptMethod)
        {
            if (!conceptMethod.IsConceptExtensionMethod)
            {
                return null;
            }
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "concept extension method should have a 'this' parameter");
            return new SynthesizedConceptExtensionShimMethod(conceptMethod, this);
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept method to
        /// one on the given default struct.
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
        /// The shim might not be valid: this must be checked before acceptance.
        /// </returns>
        private SynthesizedDefaultShimMethod TrySynthesizeDefaultShim(NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            var dstr = concept.GetDefaultStruct();
            if (dstr == null)
            {
                return null;
            }

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

            return new SynthesizedDefaultShimMethod(conceptMethod, dstr, this);
        }

        #endregion Shim synthesis
        #region Implementation checks


        /// <summary>
        /// If this is a concept instance, checks that all of its members come
        /// from concepts and agree with their original definitions.
        /// </summary>
        /// <param name="diagnostics">
        /// The diagnostics bag to extend with errors coming from excess
        /// members.
        /// </param>
        private void CheckConceptInstanceMembers(DiagnosticBag diagnostics)
        {
            // @MattWindsor91 (Concept-C# 2017)
            // This is extremely inefficient: it does a forall-exists pairwise
            // equality check across all concepts in the instance.

            if (!IsInstance || IsStandaloneInstance)
            {
                // Standalone instances would normally report _every_ member as
                // excess.
                return;
            }

            var nonExcessMembers = PooledHashSet<Symbol>.GetInstance();
            foreach (var iface in AllInterfacesNoUseSiteDiagnostics)
            {
                foreach (var ifaceMember in iface.GetMembersUnordered())
                {
                    var implMember = FindImplementationForInterfaceMember(ifaceMember);
                    if (implMember == null)
                    {
                        // Don't throw an error: for one, 'imember' might be
                        // getting shimmed; for two, we already throw the
                        // error elsewhere.
                        continue;
                    }
                    nonExcessMembers.Add(implMember);

                    if (implMember.Kind == SymbolKind.Method)
                    {
                        Debug.Assert(ifaceMember.Kind == SymbolKind.Method,
                            "implementation should have same symbol kind as interface version");
                        var implMethod = (MethodSymbol)implMember;
                        var ifaceMethod = (MethodSymbol)ifaceMember;

                        // If a method is a CEM at its concept, it should be
                        // one at its implementation, and vice versa.
                        if (implMethod.IsConceptExtensionMethod && !ifaceMethod.IsConceptExtensionMethod)
                        {
                            // CS8961: The method '{0}' is a concept extension method, but the concept '{1}' does not declare it as one.
                            diagnostics.Add(ErrorCode.ERR_CEMImplementsNonCEM, implMethod.Locations.ElementAtOrDefault(0), implMethod, iface);
                        }
                        if (!implMethod.IsConceptExtensionMethod && ifaceMethod.IsConceptExtensionMethod)
                        {
                            // CS8962: The method '{0}' is not a concept extension method, but the concept '{1}' declares it as one.                        
                            diagnostics.Add(ErrorCode.ERR_NonCEMImplementsCEM, implMethod.Locations.ElementAtOrDefault(0), implMethod, iface);
                        }
                    }
                }
            }

            CheckExcessMembers(diagnostics, nonExcessMembers);
        }

        /// <summary>
        /// Check that all of this symbol's members come from concepts.
        /// </summary>
        /// <param name="diagnostics">
        /// The diagnostics bag to extend with errors from excess members.
        /// </param>
        /// <param name="nonExcessMembers">
        /// The set of members that have already been matched to a concept.
        /// </param>
        private void CheckExcessMembers(DiagnosticBag diagnostics, PooledHashSet<Symbol> nonExcessMembers)
        {
            // If a member wasn't found during our sweep through interfaces,
            // it's an excess member.
            var excessMembers = PooledHashSet<Symbol>.GetInstance();
            foreach (var member in GetMembersUnordered())
            {
                if (member.Kind == SymbolKind.Method)
                {
                    var method = (MethodSymbol)member;
                    if (method.IsDefaultValueTypeConstructor())
                    {
                        // Ignore the implicit struct constructor:
                        // we can't get rid of it, and it's harmless anyway.
                        continue;
                    }

                    var assoc = method.AssociatedSymbol;
                    if (assoc != null && excessMembers.Contains(assoc))
                    {
                        // If we reported, for example, a property, don't
                        // re-report its accessors.
                        continue;
                    }
                }

                if (!nonExcessMembers.Contains(member))
                {
                    excessMembers.Add(member);
                    // CS8960: Concept instance member '{0}' does not match a member of any implemented concept.
                    diagnostics.Add(ErrorCode.ERR_ExcessConceptInstanceMembers, member.Locations.ElementAtOrDefault(0), member);
                }
            }
        }

        /// <summary>
        /// Is the given member an excess concept member according to
        /// the concepts in the given interface set?
        /// </summary>
        /// <param name="member">
        /// The member to check.
        /// </param>
        /// <param name="interfaces">
        /// The set of interfaces containing all concepts that
        /// <paramref name="member"/> should belong to if it is not excess.
        /// </param>
        /// <returns>
        /// True if <paramref name="member"/> is not an implementation of
        /// a method from one of the concepts in <paramref name="interfaces"/>.
        /// False otherwise.
        /// </returns>
        private bool IsExcessConceptMember(Symbol member, ImmutableArray<NamedTypeSymbol> interfaces)
        {
            foreach (var @interface in interfaces)
            {
                if (!@interface.IsConcept)
                {
                    continue;
                }

                foreach (var imember in @interface.GetMembersUnordered())
                {
                    if (FindImplementationForInterfaceMember(imember) == member)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion Implementation checks
    }
}
