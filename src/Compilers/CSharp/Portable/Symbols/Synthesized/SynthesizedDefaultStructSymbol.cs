// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A struct containing default implementations of concept methods.
    /// This is boiled down into a normal struct in the metadata.
    /// </summary>
    internal sealed class SynthesizedDefaultStructSymbol : SynthesizedContainer
    {
        /// <summary>
        /// The concept in which this default struct is located.
        /// </summary>
        private readonly SourceNamedTypeSymbol _concept;

        /// <summary>
        /// Constructs a new SynthesizedDefaultStructSymbol.
        /// </summary>
        /// <param name="name">
        /// The name of the default struct.
        /// </param>
        /// <param name="concept">
        /// The parent concept of the default struct.
        /// </param>
        public SynthesizedDefaultStructSymbol(string name, SourceNamedTypeSymbol concept)
            : base(
                  name,
                  that =>
                      // We add a type parameter for the calling witness, so the
                      // default struct can call back into it.
                      ImmutableArray<TypeParameterSymbol>.Empty
                          .Add(
                          new SynthesizedWitnessParameterSymbol(
                              // @t-mawind
                              //   need to make this not clash with any typar in
                              //   the parent scopes, hence generated name.
                              GeneratedNames.MakeAnonymousTypeParameterName("witness"),
                              Location.None,
                              0,
                              that,
                              _ => ImmutableArray.Create((TypeSymbol) concept),
                              _ => TypeParameterConstraintKind.ValueType
                          )
                      ),
                  TypeMap.Empty
              )
        {

            _concept = concept;
            // We can't get the default members here: it'll cause an infinite
            // loop...
        }

        public override Symbol ContainingSymbol => _concept;

        // @t-mawind
        //   A default struct is, of course, a struct...
        public override TypeKind TypeKind => TypeKind.Struct;

        // @t-mawind
        //   ...and, as it has no fields, its layout is specified as the
        //   minimum allowed by the CLI spec (1).
        //   This override is necessary, as otherwise the generated PE is
        //   invalid.
        internal sealed override TypeLayout Layout =>
            new TypeLayout(LayoutKind.Sequential, 1, alignment: 0);


        // @t-mawind
        //   Defaults have to be public, else they're useless.
        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        private ImmutableArray<Symbol> _members;

        public override ImmutableArray<Symbol> GetMembers()
        {
            // @t-mawind
            //   Not making this lazy results in new symbols being created every
            //   time we call GetMembers(), which is not only inefficient but
            //   breaks reference equality.
            if (_members.IsDefault)
            {
                var mb = ArrayBuilder<Symbol>.GetInstance();
                mb.AddRange(base.GetMembers());

                // @t-mawind
                //   This is slightly wrong, but we don't have any syntax to
                //   cling onto apart from this...
                var binder = DeclaringCompilation.GetBinder(ContainingType.GetNonNullSyntaxNode());

                var diagnostics = DiagnosticBag.GetInstance();

                var memberRefs = _concept.GetConceptDefaultMethods();
                foreach (var memberRef in memberRefs)
                {
                    Debug.Assert(memberRef != null, "should not have got a null member reference here");
                    Debug.Assert(memberRef.GetSyntax() != null, "should not have got a syntax-less member reference here");

                    switch (memberRef.GetSyntax().Kind())
                    {
                        case SyntaxKind.MethodDeclaration:
                            var ms = (MethodDeclarationSyntax)memberRef.GetSyntax();
                            mb.Add(SourceOrdinaryMethodSymbol.CreateMethodSymbol(this, binder, ms, diagnostics));
                            break;
                        case SyntaxKind.OperatorDeclaration:
                            var os = (OperatorDeclarationSyntax)memberRef.GetSyntax();
                            mb.Add(SourceUserDefinedOperatorSymbol.CreateUserDefinedOperatorSymbol(this, os, diagnostics));
                            break;
                        default:
                            throw ExceptionUtilities.UnexpectedValue(memberRef.GetSyntax().Kind());
                    }
                }

                AddDeclarationDiagnostics(diagnostics);

                ImmutableInterlocked.InterlockedInitialize(ref _members, mb.ToImmutableAndFree());
            }
            return _members;
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            // @t-mawind TODO: slow and ugly.

            var mb = ArrayBuilder<Symbol>.GetInstance();
            mb.AddRange(base.GetMembers(name));

            foreach (var m in GetMembers())
            {
                if (m.Name == name) mb.Add(m);
            }

            return mb.ToImmutableAndFree();
        }

        internal override bool IsDefaultStruct => true;

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            CSharpCompilation compilation = DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Concepts_ConceptDefaultAttribute__ctor));
        }
    }
}
