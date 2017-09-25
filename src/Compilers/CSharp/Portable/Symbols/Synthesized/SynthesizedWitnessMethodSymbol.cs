// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Symbol representing a member belonging to a concept, which has been
    /// accessed through a concept witness.
    /// <para>
    /// The main goal of this class is to mark the method so invocations
    /// of it can be dispatched properly during binding.  This is a rather
    /// hacky way of doing this, to say the least.
    /// </para>
    /// </summary>
    internal sealed class SynthesizedWitnessMethodSymbol : WrappedMethodSymbol
    {
        /// <summary>
        /// The witness 'owning' the concept method.
        /// </summary>
        private TypeSymbol _parent;

        /// <summary>
        /// The concept method to wrap.
        /// </summary>
        private MethodSymbol _method;

        /// <summary>
        /// Constructs a new <see cref="SynthesizedWitnessMethodSymbol"/>.
        /// </summary>
        /// <param name="method">
        /// The concept method to wrap.
        /// </param>
        /// <param name="parent">
        /// The witness 'owning' the concept method.
        /// </param>
        internal SynthesizedWitnessMethodSymbol(MethodSymbol method, TypeSymbol parent)
            : base()
        {
            Debug.Assert(parent != null, "synthesized witness method must have a parent");
            Debug.Assert(parent.IsConceptWitness || parent.IsInstanceType(), "parent of a synthesised witness method must be an instance or witness");

            _method = method;
            _parent = parent;
        }

        /// <summary>
        /// Gets the type parameter of the witness from which this method is
        /// being called.
        /// </summary>
        internal TypeSymbol Parent => _parent;

        public override MethodSymbol UnderlyingMethod => _method;

        // @t-mawind
        //   The following are things WrappedMethodSymbol doesn't give us for
        //   free, and are probably incorrect.

        public override MethodSymbol OriginalDefinition => UnderlyingMethod.OriginalDefinition;

        public override Symbol ContainingSymbol => UnderlyingMethod.ContainingSymbol;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
            => ImmutableArray<SyntaxReference>.Empty;

        public sealed override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<Location> Locations
            => ImmutableArray<Location>.Empty;

        public override bool ReturnsVoid => UnderlyingMethod.ReturnsVoid;

        public override TypeSymbol ReturnType => UnderlyingMethod.ReturnType;

        public override ImmutableArray<TypeSymbol> TypeArguments => UnderlyingMethod.TypeArguments;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => UnderlyingMethod.TypeParameters;

        public override ImmutableArray<ParameterSymbol> Parameters => UnderlyingMethod.Parameters;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => UnderlyingMethod.ExplicitInterfaceImplementations;

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers => UnderlyingMethod.ReturnTypeCustomModifiers;

        public override Symbol AssociatedSymbol => UnderlyingMethod.AssociatedSymbol;
        internal override bool IsExplicitInterfaceImplementation => UnderlyingMethod.IsExplicitInterfaceImplementation;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingMethod.RefCustomModifiers;

        // TODO: this is probably wrong, as we have no syntax.
        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => UnderlyingMethod.CalculateLocalSyntaxOffset(localPosition, localTree);

        public override ImmutableArray<CSharpAttributeData> GetAttributes() => UnderlyingMethod.GetAttributes();

        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes() => UnderlyingMethod.GetReturnTypeAttributes();
    }
}
