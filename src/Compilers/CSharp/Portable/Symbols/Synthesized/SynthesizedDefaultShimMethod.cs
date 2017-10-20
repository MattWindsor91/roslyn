// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized concept instance method that, when generated,
    /// calls into a corresponding method on a concept default struct.
    /// </summary>
    internal sealed class SynthesizedDefaultShimMethod : SynthesizedInstanceShimMethod
    {
        /// <summary>
        /// The default struct into which this method will call.
        /// </summary>
        private NamedTypeSymbol _defaultStruct;

        public SynthesizedDefaultShimMethod(MethodSymbol conceptMethod, NamedTypeSymbol defaultStruct, NamedTypeSymbol implementingType)
            : base(conceptMethod, implementingType)
        {
            _defaultStruct = defaultStruct;
        }

        protected override BoundExpression GenerateReceiver(SyntheticBoundNodeFactory f)
        {
            // The receiver has one argument, namely the calling witness.
            // We generate an empty local for it, and then call into that local.
            // We then place the local into the block.
            var recvType = _defaultStruct.Construct(ImmutableArray.Create<TypeSymbol>(ContainingType));
            var recvLocal = f.SynthesizedLocal(recvType, syntax: f.Syntax, kind: SynthesizedLocalKind.ConceptDictionary);
            return f.Local(recvLocal);
        }

        protected override ImmutableArray<LocalSymbol> GenerateLocals(SyntheticBoundNodeFactory f, BoundExpression receiver)
        {
            Debug.Assert(receiver.Kind == BoundKind.Local,
                "should have not been able to create a non-local receiver here");
            return ImmutableArray.Create(((BoundLocal)receiver).LocalSymbol);
        }

        protected override (ImmutableArray<BoundExpression> args, ImmutableArray<RefKind> refs) GenerateArguments(SyntheticBoundNodeFactory f)
        {
            var argsB = ArrayBuilder<BoundExpression>.GetInstance();
            var refsB = ArrayBuilder<RefKind>.GetInstance();
            foreach (var p in ImplementingMethod.Parameters)
            {
                argsB.Add(f.Parameter(p));
                refsB.Add(p.RefKind);
            }
            return (argsB.ToImmutableAndFree(), refsB.ToImmutableAndFree());
        }
    }
}
