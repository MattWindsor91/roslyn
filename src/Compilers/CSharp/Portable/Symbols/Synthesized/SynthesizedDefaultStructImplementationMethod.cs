// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedDefaultStructImplementationMethod : SynthesizedImplementationForwardingMethod
    {
        // @t-mawind
        //   The entire existence of this class is a horrific hack.

        public SynthesizedDefaultStructImplementationMethod(MethodSymbol conceptMethod, NamedTypeSymbol implementingType)
            : base(conceptMethod, conceptMethod, implementingType)
        {
        }

        public override Accessibility DeclaredAccessibility => Accessibility.Public;
        public override MethodKind MethodKind => ImplementingMethod.MethodKind;

        // @t-mawind TODO: should this be an explicit implementation?
        //   if it is, we need to figure out how to make it visible to the
        //   binder.
        internal override bool IsExplicitInterfaceImplementation => false;
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override string Name => ImplementingMethod.Name;
        public override string MetadataName => ImplementingMethod.MetadataName;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var concept = ImplementingMethod.ContainingType;
            var conceptLoc = concept.Locations.IsEmpty ? Location.None : concept.Locations[0];
            // TODO: wrong location?

            Debug.Assert(concept.IsConcept, "Tried to synthesise default struct implementation on a non-concept interface");
            
            var instance = ContainingType;
            var instanceLoc = instance.Locations.IsEmpty ? Location.None : instance.Locations[0];
            // TODO: wrong location?

            Debug.Assert(instance.IsInstance, "Tried to synthesise default struct implementation for a non-instance");

            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = OriginalDefinition;

            try
            {

                // Now try to find the default struct using the instance's scope...
                var binder = new BinderFactory(compilationState.Compilation, instance.GetNonNullSyntaxNode().SyntaxTree).GetBinder(instance.GetNonNullSyntaxNode());

                var defs = concept.GetDefaultStruct();
                if (defs == null)
                {
                    diagnostics.Add(ErrorCode.ERR_ConceptMethodNotImplementedAndNoDefault, instanceLoc, instance.Name, concept.Name, ImplementingMethod.ToDisplayString());
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                Debug.Assert(defs.Arity == concept.Arity + 1, "should have already pre-checked default struct arity");
                Debug.Assert(defs.TypeParameters[defs.Arity - 1].IsConceptWitness, "should have already pre-checked default struct witness parameter");

                var newTypeArguments = GenerateDefaultTypeArguments();
                Debug.Assert(newTypeArguments.Length == concept.TypeArguments.Length + 1,
                    "Conversion from concept type parameters to default struct lost or gained some entries.");

                // Now make the receiver for the call.  As usual, it's a default().
                var recvType = defs.Construct(newTypeArguments);
                var receiver = F.Default(recvType);

                var arguments = GenerateInnerCallArguments(F);
                Debug.Assert(arguments.Length == ImplementingMethod.Parameters.Length,
                    "Conversion from parameters to arguments lost or gained some entries.");

                var call = F.MakeInvocationExpression(BinderFlags.None, F.Syntax, receiver, ImplementingMethod.Name, arguments, diagnostics, ImplementingMethod.TypeArguments);
                if (call.HasErrors)
                {
                    F.CloseMethod(F.ThrowNull());
                    return;
                }

                // If whichever call we end up making returns void, then we
                // can't just return its result; instead, we have to do the
                // call on its own _then_ return.
                BoundBlock block;
                if (call.Type.SpecialType == SpecialType.System_Void)
                {
                    block = F.Block(F.ExpressionStatement(call), F.Return());
                }
                else
                {
                    block = F.Block(F.Return(call));
                }

                F.CloseMethod(block);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }

        /// <summary>
        /// Generates the correct set of type arguments for the default struct.
        /// <para>
        /// This is the same as the concept arguments list, plus one: the
        /// instance that is calling into the default struct.
        /// </para>
        /// </summary>
        /// <returns>
        /// The list of type arguments for the default struct.
        /// </returns>
        private ImmutableArray<TypeSymbol> GenerateDefaultTypeArguments()
        {
            var newTypeArgumentsB = ArrayBuilder<TypeSymbol>.GetInstance();
            newTypeArgumentsB.AddRange(ImplementingMethod.ContainingType.TypeArguments);
            // This should be the extra witness parameter, if the default
            // struct is well-formed,
            newTypeArgumentsB.Add(ContainingType);

            return newTypeArgumentsB.ToImmutableAndFree();
        }

        /// <summary>
        /// Converts the formal parameters of this method into the
        /// arguments of the inner call.
        /// </summary>
        /// <param name="f">
        /// The factory used to generate the arguments.
        /// </param>
        /// <returns>
        /// A list of bound inner-call arguments.
        /// </returns>
        private ImmutableArray<BoundExpression> GenerateInnerCallArguments(SyntheticBoundNodeFactory f)
        {
            var argumentsB = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var p in ImplementingMethod.Parameters) argumentsB.Add(f.Parameter(p));
            return argumentsB.ToImmutableAndFree();
        }
    }
}
