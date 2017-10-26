// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A struct containing default implementations of concept methods.
    /// This is boiled down into a normal struct in the metadata.
    /// </summary>
    internal sealed class SynthesizedDefaultStructSymbol : SynthesizedConceptHelperStructSymbol
    {
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
                  concept,
                  that =>
                      // We add a type parameter for the calling witness, so the
                      // default struct can call back into it.
                      ImmutableArray<TypeParameterSymbol>.Empty
                          .Add(
                          new SynthesizedWitnessParameterSymbol(
                              // @t-mawind
                              //   need to make this not clash with any typar in
                              //   the parent scopes, hence generated name.
                              GeneratedNames.WitnessTypeParameterName(),
                              Location.None,
                              0,
                              that,
                              _ => ImmutableArray.Create((TypeSymbol)concept),
                              _ => TypeParameterConstraintKind.ValueType
                          )
                      ),
                  TypeMap.Empty
              )
        { }
        
        internal override bool IsDefaultStruct => true;

        protected override void MakeMembers(ArrayBuilder<Symbol> mb, Binder binder, DiagnosticBag diagnostics)
        {
            var memberRefs = ((SourceNamedTypeSymbol)ContainingSymbol).GetConceptDefaultMethods();
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
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            CSharpCompilation compilation = DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Concepts_ConceptDefaultAttribute__ctor));
        }
    }
}
