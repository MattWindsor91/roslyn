// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

        /// <summary>
        /// Checks whether this shim will generate a correct call when
        /// synthesised.
        /// </summary>
        /// <returns>
        /// True if, and only if, the shim's inner call will raise no errors
        /// when synthesised.
        /// </returns>
        internal bool IsValid()
        {
            // TODO(@MattWindsor91): perhaps there are some more lightweight
            //     checks we can do here.

            var ignore = new DiagnosticBag();
            var ignore2 = new HashSet<DiagnosticInfo>();
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), new TypeCompilationState(null, DeclaringCompilation, null), ignore);

            try
            {
                var receiver = GenerateReceiver(F);
                var arguments = GenerateInnerCallArguments(F);
                var call = GenerateCall(F, receiver, arguments, ignore);
                if (call.HasErrors)
                {
                    return false;
                }
                // Make sure the return type of the call lines up perfectly.
                // TODO(@MattWindsor91): is this too restrictive?
                return DeclaringCompilation.Conversions.ClassifyConversionFromExpression(call, ReturnType, ref ignore2).IsIdentity;
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember)
            {
                return false;
            }
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            Debug.Assert(0 < ParameterCount,
                "method should have at least one parameter, eg. its 'this' parameter");

            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics)
            {
                CurrentMethod = OriginalDefinition
            };

            try
            {
                var receiver = GenerateReceiver(F);
                var locals = GenerateLocals(F, receiver);
                var arguments = GenerateInnerCallArguments(F);
                var call = GenerateCall(F, receiver, arguments, diagnostics);
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

        protected virtual BoundExpression GenerateCall(SyntheticBoundNodeFactory f, BoundExpression receiver, ImmutableArray<BoundExpression> arguments, DiagnosticBag diagnostics)
        {
            return f.MakeInvocationExpression(BinderFlags.InShim, f.Syntax, receiver, Name, arguments, diagnostics, TypeArguments);
        }
    }
}
