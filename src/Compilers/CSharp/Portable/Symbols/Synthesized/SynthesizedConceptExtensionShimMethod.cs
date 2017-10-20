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
    internal sealed class SynthesizedConceptExtensionShimMethod : SynthesizedInstanceShimMethod
    {
        public SynthesizedConceptExtensionShimMethod(MethodSymbol conceptMethod, NamedTypeSymbol implementingType)
            : base(conceptMethod, implementingType)
        {
        }

        protected override BoundExpression GenerateReceiver(SyntheticBoundNodeFactory f) =>
            // The receiver for the call is the first parameter to this method,
            // as we're bridging from a concept extension method to a real
            // instance method.
            f.Parameter(Parameters[0]);

        protected override ImmutableArray<LocalSymbol> GenerateLocals(SyntheticBoundNodeFactory f, BoundExpression _) =>
            ImmutableArray<LocalSymbol>.Empty;

        protected override (ImmutableArray<BoundExpression> args, ImmutableArray<RefKind> refs) GenerateArguments(SyntheticBoundNodeFactory f)
        {
            // The first argument becomes the receiver.
            var argsB = ArrayBuilder<BoundExpression>.GetInstance();
            var refsB = ArrayBuilder<RefKind>.GetInstance();
            for (int i = 1; i < ParameterCount; i++)
            {
                argsB.Add(f.Parameter(Parameters[i]));
                refsB.Add(Parameters[i].RefKind);
            }
            var args = argsB.ToImmutableAndFree();
            var refs = refsB.ToImmutableAndFree();
            Debug.Assert(args.Length == Parameters.Length - 1,
                "Conversion from parameters to arguments lost or gained some entries.");
            return (args, refs);
        }
    }
}
