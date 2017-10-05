// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized concept instance method that, when generated,
    /// calls into a corresponding method on its this-parameter.
    /// </summary>
    internal sealed class SynthesizedConceptExtensionForwardingImplementationMethod : SynthesizedImplementationForwardingMethod
    {
        public SynthesizedConceptExtensionForwardingImplementationMethod(MethodSymbol conceptMethod, NamedTypeSymbol implementingType)
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
            Debug.Assert(0 < ParameterCount,
                "method should have at least one parameter, eg. its 'this' parameter");

            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentMethod = OriginalDefinition;

            try
            {
                // The receiver for the call is the first parameter to this method,
                // as we're bridging from a concept extension method to a real
                // instance method.
                var receiver = F.Parameter(Parameters[0]);

                var arguments = GenerateInnerCallArguments(F);
                Debug.Assert(arguments.Length == Parameters.Length - 1,
                    "Conversion from parameters to arguments lost or gained some entries.");

                var call = F.MakeInvocationExpression(BinderFlags.None, F.Syntax, receiver, Name, arguments, diagnostics, TypeArguments);
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
            for (int i = 1; i < ParameterCount; i++)
            {
                argumentsB.Add(f.Parameter(Parameters[i]));
            }
            return argumentsB.ToImmutableAndFree();
        }
    }
}
