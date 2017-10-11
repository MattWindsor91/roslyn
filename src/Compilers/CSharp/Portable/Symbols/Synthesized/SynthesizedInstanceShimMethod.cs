// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized concept instance method that calls out to some other
    /// method (eg. a default method, or a class method).
    /// </summary>
    internal abstract class SynthesizedInstanceShimMethod : SynthesizedImplementationForwardingMethod
    {
        public SynthesizedInstanceShimMethod(MethodSymbol conceptMethod, NamedTypeSymbol implementingType)
            : base(conceptMethod, conceptMethod, implementingType)
        {
        }

        /// <summary>
        /// Shim methods are always public.
        /// </summary>
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
                var receiver = GenerateReceiver(F);
                var locals = GenerateLocals(F, receiver);
                var arguments = GenerateInnerCallArguments(F);

                var call = F.MakeInvocationExpression(BinderFlags.None, F.Syntax, receiver, Name, arguments, diagnostics, TypeArguments, allowInvokingSpecialMethod: true);
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
                    block = F.Block(locals, F.ExpressionStatement(call), F.Return());
                }
                else
                {
                    block = F.Block(locals, F.Return(call));
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
        /// Generates the receiver for this shim call.
        /// </summary>
        /// <param name="f">
        /// The factory used to generate the receiver.
        /// </param>
        /// <returns>
        /// The receiver for the shim call.
        /// </returns>
        protected abstract BoundExpression GenerateReceiver(SyntheticBoundNodeFactory f);

        /// <summary>
        /// Generates the locals list for this shim call.
        /// </summary>
        /// <param name="f">
        /// The factory used to generate the locals.
        /// </param>
        /// <param name="receiver">
        /// The receiver, in case it needs to be turned into a local.
        /// </param>
        /// <returns>
        /// The locals list for this shim call.
        /// </returns>
        protected abstract ImmutableArray<LocalSymbol> GenerateLocals(SyntheticBoundNodeFactory f, BoundExpression receiver);

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
        protected abstract ImmutableArray<BoundExpression> GenerateInnerCallArguments(SyntheticBoundNodeFactory f);
    }
}
