// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Symbol representing a property belonging to a concept, which has been
    /// accessed through a concept witness.
    /// <para>
    /// The main goal of this class is to mark the method so invocations
    /// of it can be dispatched properly during binding.  This is a rather
    /// rough way of doing this, to say the least.
    /// </para>
    /// </summary>
    internal sealed class SynthesizedWitnessPropertySymbol : WrappedPropertySymbol
    {
        /// <summary>
        /// The witness 'owning' the concept method.
        /// </summary>
        private TypeParameterSymbol _parent;


        /// <summary>
        /// Constructs a new <see cref="SynthesizedWitnessPropertySymbol"/>.
        /// </summary>
        /// <param name="property">
        /// The concept property to wrap.
        /// </param>
        /// <param name="parent">
        /// The witness 'owning' the concept property.
        /// </param>
        internal SynthesizedWitnessPropertySymbol(PropertySymbol property, TypeParameterSymbol parent)
            : base(property)
        {
            Debug.Assert(parent.IsConceptWitness);

            _parent = parent;
        }

        /// <summary>
        /// Gets the type parameter of the witness from which this method is
        /// being called.
        /// </summary>
        internal TypeParameterSymbol Parent => _parent;

        // @MattWindsor91 (Concept-C# 2017)
        //   The following are things WrappedPropertySymbol doesn't give us for
        //   free, and are probably incorrect.

        public override PropertySymbol OriginalDefinition => UnderlyingProperty.OriginalDefinition;

        public override Symbol ContainingSymbol => UnderlyingProperty.ContainingSymbol;

        public sealed override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<Location> Locations => UnderlyingProperty.Locations;

        internal override bool IsExplicitInterfaceImplementation => UnderlyingProperty.IsExplicitInterfaceImplementation;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => UnderlyingProperty.RefCustomModifiers;

        public override TypeSymbol Type => UnderlyingProperty.Type;

        public override ImmutableArray<CustomModifier> TypeCustomModifiers => UnderlyingProperty.TypeCustomModifiers;

        public override ImmutableArray<ParameterSymbol> Parameters => UnderlyingProperty.Parameters;

        public override MethodSymbol GetMethod => UnderlyingProperty.GetMethod;

        public override MethodSymbol SetMethod => UnderlyingProperty.SetMethod;

        internal override bool MustCallMethodsDirectly => UnderlyingProperty.MustCallMethodsDirectly;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => UnderlyingProperty.ExplicitInterfaceImplementations;

        public override ImmutableArray<CSharpAttributeData> GetAttributes() => UnderlyingProperty.GetAttributes();
    }
}
