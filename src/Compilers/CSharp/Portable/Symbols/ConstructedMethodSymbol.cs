// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // @MattWindsor91 (Concept-C# 2017)
    //
    // Unsealed to allow RetargetedConstructedMethodSymbol to descend from this

    internal class ConstructedMethodSymbol : SubstitutedMethodSymbol
    {
        private readonly ImmutableArray<TypeSymbol> _typeArguments;

        internal ConstructedMethodSymbol(MethodSymbol constructedFrom, ImmutableArray<TypeSymbol> typeArguments)
            : base(containingSymbol: constructedFrom.ContainingType,
                   map: new TypeMap(constructedFrom.ContainingType, ((MethodSymbol)constructedFrom.OriginalDefinition).TypeParameters, typeArguments.SelectAsArray(TypeMap.TypeSymbolAsTypeWithModifiers)),
                   originalDefinition: (MethodSymbol)constructedFrom.OriginalDefinition,
                   constructedFrom: constructedFrom)
        {
            _typeArguments = typeArguments;
        }

        // @MattWindsor (Concept-C# 2017)
        //
        // Added new constructor to allow retargeting the contained
        // type.

        internal ConstructedMethodSymbol(MethodSymbol constructedFrom, ImmutableArray<TypeSymbol> typeArguments, NamedTypeSymbol newContainingType)
        : base(containingSymbol: newContainingType,
           map: new TypeMap(newContainingType, (constructedFrom.OriginalDefinition).TypeParameters, typeArguments.SelectAsArray(TypeMap.TypeSymbolAsTypeWithModifiers)),
           originalDefinition: constructedFrom.OriginalDefinition,
           constructedFrom: constructedFrom)
        {
            _typeArguments = typeArguments;
        }

        public override ImmutableArray<TypeSymbol> TypeArguments
        {
            get
            {
                return _typeArguments;
            }
        }

        public override bool IsTupleMethod
        {
            get
            {
                return ConstructedFrom.IsTupleMethod;
            }
        }

        public override MethodSymbol TupleUnderlyingMethod
        {
            get
            {
                return ConstructedFrom.TupleUnderlyingMethod?.Construct(_typeArguments);
            }
        }
    }
}
