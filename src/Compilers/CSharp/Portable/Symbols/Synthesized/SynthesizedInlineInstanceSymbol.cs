// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A struct containing shim implementations of any concepts declared
    /// as bases of a non-concept, non-interface symbol.
    /// </summary>
    internal sealed class SynthesizedInlineInstanceSymbol : SynthesizedConceptHelperStructSymbol
    {
        /// <summary>
        /// Constructs a new SynthesizedInlineInstanceSymbol.
        /// </summary>
        /// <param name="name">
        /// The name of the inline instance struct.
        /// </param>
        /// <param name="parent">
        /// The symbol doing inline implementation of concepts.
        /// </param>
        public SynthesizedInlineInstanceSymbol(string name, SourceNamedTypeSymbol parent)
            : base(name, parent, _ => ImmutableArray<TypeParameterSymbol>.Empty, TypeMap.Empty)
        {}

        internal override ImmutableArray<NamedTypeSymbol> GetDeclaredInterfaces(ConsList<Symbol> basesBeingResolved)
        {
            return ((SourceNamedTypeSymbol)ContainingSymbol).GetConceptsForInlineInstances(basesBeingResolved);
        }

        private ImmutableArray<NamedTypeSymbol> _lazyInterfaces;
        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<Symbol> basesBeingResolved)
        {
            // NOTE: this is mostly copied from SourceNamedTypeSymbol_Bases.

            if (_lazyInterfaces.IsDefault)
            {
                if (basesBeingResolved != null && basesBeingResolved.ContainsReference(this.OriginalDefinition))
                {
                    return ImmutableArray<NamedTypeSymbol>.Empty;
                }

                var diagnostics = DiagnosticBag.GetInstance();
                var acyclicInterfaces = MakeAcyclicInterfaces(basesBeingResolved, diagnostics);
                if (ImmutableInterlocked.InterlockedCompareExchange(ref _lazyInterfaces, acyclicInterfaces, default(ImmutableArray<NamedTypeSymbol>)).IsDefault)
                {
                    AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            return _lazyInterfaces;
        }

        private ImmutableArray<NamedTypeSymbol> MakeAcyclicInterfaces(ConsList<Symbol> basesBeingResolved, DiagnosticBag diagnostics)
        {
            // NOTE: this is mostly copied from SourceNamedTypeSymbol_Bases.            
            var declaredInterfaces = GetDeclaredInterfaces(basesBeingResolved: basesBeingResolved);

            var result = ArrayBuilder<NamedTypeSymbol>.GetInstance();
            foreach (var t in declaredInterfaces)
            {
                if (BaseTypeAnalysis.InterfaceDependsOn(depends: t, on: this))
                {
                    result.Add(new ExtendedErrorTypeSymbol(t, LookupResultKind.NotReferencable,
                        diagnostics.Add(ErrorCode.ERR_CycleInInterfaceInheritance, Locations[0], this, t)));
                    continue;
                }
                else
                {
                    result.Add(t);
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;

                if (t.DeclaringCompilation != DeclaringCompilation)
                {
                    t.AddUseSiteDiagnostics(ref useSiteDiagnostics);

                    foreach (var @interface in t.AllInterfacesNoUseSiteDiagnostics)
                    {
                        if (@interface.DeclaringCompilation != DeclaringCompilation)
                        {
                            @interface.AddUseSiteDiagnostics(ref useSiteDiagnostics);
                        }
                    }
                }

                if (!useSiteDiagnostics.IsNullOrEmpty())
                {
                    diagnostics.Add(Locations[0], useSiteDiagnostics);
                }
            }

            return result.ToImmutableAndFree();
        }

        protected override void MakeMembers(ArrayBuilder<Symbol> mb, Binder binder, DiagnosticBag diagnostics)
        {
            // TODO(@MattWindsor91): handle duplicate methods

            foreach (var concept in ((SourceNamedTypeSymbol)ContainingSymbol).GetConceptsForInlineInstances(null))
            {
                foreach (var member in concept.GetMembersUnordered())
                {
                    // TODO(@MattWindsor91): properties
                    if (member.Kind != SymbolKind.Method)
                    {
                        // TODO(@MattWindsor91): better error?
                        diagnostics.Add(ErrorCode.ERR_InlineInstanceNonMethodMember, ContainingSymbol.GetNonNullSyntaxNode().Location, member);
                        continue;
                    }
                    var shim = TrySynthesizeInstanceShim(concept, (MethodSymbol)member, diagnostics);
                    if (shim != null)
                    {
                        mb.Add(shim);
                    }
                }
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            CSharpCompilation compilation = DeclaringCompilation;
            // Both of these are needed.
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Concepts_ConceptInstanceAttribute__ctor));
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Concepts_ConceptInlineInstanceAttribute__ctor));
        }

        internal override bool IsInstance => true;
        internal override bool IsInlineInstanceStruct => true;

        internal void CheckConceptImplementations(DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            // TODO(@MattWindsor91): Most of this is copied from
            //     SourceMemberContainerSymbol.ComputeInterfaceImplementations,
            //     and may not be relevant or optimal.

            ImmutableHashSet<NamedTypeSymbol> interfacesAndTheirBases = InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics;

            foreach (var concept in AllInterfacesNoUseSiteDiagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!interfacesAndTheirBases.Contains(concept))
                {
                    continue;
                }

                foreach (var conceptMember in concept.GetMembersUnordered())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Only require implementations for members that can be implemented in C#.
                    SymbolKind conceptMemberKind = conceptMember.Kind;
                    switch (conceptMemberKind)
                    {
                        case SymbolKind.Method:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                            if (conceptMember.IsStatic)
                            {
                                continue;
                            }
                            break;
                        default:
                            continue;
                    }

                    var implementingMemberAndDiagnostics = FindImplementationForInterfaceMemberWithDiagnostics(conceptMember);
                    // TODO(@MattWindsor91): Probably incorrect
                    if (implementingMemberAndDiagnostics.Diagnostics.Any())
                    {
                        diagnostics.AddRange(implementingMemberAndDiagnostics.Diagnostics);
                    } else if (implementingMemberAndDiagnostics.Symbol == null)
                    {
                        // Suppress for bogus properties and events and for indexed properties.
                        if (!conceptMember.MustCallMethodsDirectly() && !conceptMember.IsIndexedProperty())
                        {
                            DiagnosticInfo useSiteDiagnostic = conceptMember.GetUseSiteDiagnostic();

                            if (useSiteDiagnostic != null && useSiteDiagnostic.DefaultSeverity == DiagnosticSeverity.Error)
                            {
                                // TODO(@MattWindsor91): location is wrong.
                                diagnostics.Add(useSiteDiagnostic, ContainingSymbol.GetNonNullSyntaxNode().Location);
                            }
                            else
                            {
                                diagnostics.Add(ErrorCode.ERR_InlineInstanceMissingMember, ContainingSymbol.GetNonNullSyntaxNode().Location, ContainingSymbol, concept, conceptMember);
                            }
                        }
                    }
                }
            }
        }
    }
}
