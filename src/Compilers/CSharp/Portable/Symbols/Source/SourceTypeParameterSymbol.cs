// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class for type and method type parameters.
    /// </summary>
    internal abstract class SourceTypeParameterSymbolBase : TypeParameterSymbol, IAttributeTargetSymbol
    {
        private readonly ImmutableArray<SyntaxReference> _syntaxRefs;
        private readonly ImmutableArray<Location> _locations;
        private readonly string _name;
        private readonly short _ordinal;

        private SymbolCompletionState _state;
        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;
        private TypeParameterBounds _lazyBounds = TypeParameterBounds.Unset;

        protected SourceTypeParameterSymbolBase(string name, int ordinal, ImmutableArray<Location> locations, ImmutableArray<SyntaxReference> syntaxRefs)
        {
            Debug.Assert(!syntaxRefs.IsEmpty);

            _name = name;
            _ordinal = (short)ordinal;
            _locations = locations;
            _syntaxRefs = syntaxRefs;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _syntaxRefs;
            }
        }

        internal ImmutableArray<SyntaxReference> SyntaxReferences
        {
            get
            {
                return _syntaxRefs;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return VarianceKind.None;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        internal override ImmutableArray<TypeSymbol> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.ConstraintTypes : ImmutableArray<TypeSymbol>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.Interfaces : ImmutableArray<NamedTypeSymbol>.Empty;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.EffectiveBaseClass : this.GetDefaultBaseType();
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            var bounds = this.GetBounds(inProgress);
            return (bounds != null) ? bounds.DeducedBaseType : this.GetDefaultBaseType();
        }

        internal ImmutableArray<SyntaxList<AttributeListSyntax>> MergedAttributeDeclarationSyntaxLists
        {
            get
            {
                var mergedAttributesBuilder = ArrayBuilder<SyntaxList<AttributeListSyntax>>.GetInstance();

                foreach (var syntaxRef in _syntaxRefs)
                {
                    var syntax = (TypeParameterSyntax)syntaxRef.GetSyntax();
                    mergedAttributesBuilder.Add(syntax.AttributeLists);
                }

                var sourceMethod = this.ContainingSymbol as SourceOrdinaryMethodSymbol;
                if ((object)sourceMethod != null && sourceMethod.IsPartial)
                {
                    var implementingPart = sourceMethod.SourcePartialImplementation;
                    if ((object)implementingPart != null)
                    {
                        var typeParameter = (SourceTypeParameterSymbolBase)implementingPart.TypeParameters[_ordinal];
                        mergedAttributesBuilder.AddRange(typeParameter.MergedAttributeDeclarationSyntaxLists);
                    }
                }

                return mergedAttributesBuilder.ToImmutableAndFree();
            }
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.TypeParameter; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get { return AttributeLocation.TypeParameter; }
        }

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        /// <remarks>
        /// NOTE: This method should always be kept as a sealed override.
        /// If you want to override attribute binding logic for a sub-class, then override <see cref="GetAttributesBag"/> method.
        /// </remarks>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag().Attributes;
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal virtual CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            if (_lazyCustomAttributesBag == null || !_lazyCustomAttributesBag.IsSealed)
            {
                bool lazyAttributesStored = false;

                var sourceMethod = this.ContainingSymbol as SourceOrdinaryMethodSymbol;
                if ((object)sourceMethod == null || (object)sourceMethod.SourcePartialDefinition == null)
                {
                    lazyAttributesStored = LoadAndValidateAttributes(
                        OneOrMany.Create(this.MergedAttributeDeclarationSyntaxLists),
                        ref _lazyCustomAttributesBag);
                }
                else
                {
                    var typeParameter = (SourceTypeParameterSymbolBase)sourceMethod.SourcePartialDefinition.TypeParameters[_ordinal];
                    CustomAttributesBag<CSharpAttributeData> attributesBag = typeParameter.GetAttributesBag();

                    lazyAttributesStored = Interlocked.CompareExchange(ref _lazyCustomAttributesBag, attributesBag, null) == null;
                }

                if (lazyAttributesStored)
                {
                    _state.NotePartComplete(CompletionPart.Attributes);
                }
            }

            return _lazyCustomAttributesBag;
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            if (ReferenceEquals(_lazyBounds, TypeParameterBounds.Unset))
            {
                EnsureAllConstraintsAreResolved(this.ContainerTypeParameters);
            }
        }

        protected abstract ImmutableArray<TypeParameterSymbol> ContainerTypeParameters
        {
            get;
        }

        private TypeParameterBounds GetBounds(ConsList<TypeParameterSymbol> inProgress)
        {
            Debug.Assert(!inProgress.ContainsReference(this));
            Debug.Assert(!inProgress.Any() || ReferenceEquals(inProgress.Head.ContainingSymbol, this.ContainingSymbol));

            if (ReferenceEquals(_lazyBounds, TypeParameterBounds.Unset))
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var bounds = this.ResolveBounds(inProgress, diagnostics);

                if (ReferenceEquals(Interlocked.CompareExchange(ref _lazyBounds, bounds, TypeParameterBounds.Unset), TypeParameterBounds.Unset))
                {
                    this.CheckConstraintTypeConstraints(diagnostics);
                    this.AddDeclarationDiagnostics(diagnostics);
                    _state.NotePartComplete(CompletionPart.TypeParameterConstraints);
                }

                diagnostics.Free();
            }
            return _lazyBounds;
        }

        protected abstract TypeParameterBounds ResolveBounds(ConsList<TypeParameterSymbol> inProgress, DiagnosticBag diagnostics);

        /// <summary>
        /// Determines whether this type parameter had the 'implicit' keyword set.
        /// </summary>
        /// <returns>
        /// True iff the source of this type parameter carried an 'implicit'
        /// keyword.
        /// </returns>
        private bool WasDeclaredImplicit()
        {
            foreach (var synRef in SyntaxReferences)
            {
                var syn = synRef.GetSyntax() as TypeParameterSyntax;
                Debug.Assert(syn != null, "somehow got non-type parameter syntax");
                if (syn.ImplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword) return true;
            }

            return false;
        }

        /// <summary>
        /// Check constraints of generic types referenced in constraint types. For instance,
        /// with "interface I&lt;T&gt; where T : I&lt;T&gt; {}", check T satisfies constraints
        /// on I&lt;T&gt;. Those constraints are not checked when binding ConstraintTypes
        /// since ConstraintTypes has not been set on I&lt;T&gt; at that point.
        /// </summary>
        private void CheckConstraintTypeConstraints(DiagnosticBag diagnostics)
        {
            var constraintTypes = this.ConstraintTypesNoUseSiteDiagnostics;
            if (constraintTypes.Length == 0)
            {
                return;
            }

            var corLibrary = this.ContainingAssembly.CorLibrary;
            var conversions = new TypeConversions(corLibrary);
            var location = _locations[0];

            // @t-mawind
            //   A concept parameter with no constraint types is pointless, so
            //   don't allow it.
            if (WasDeclaredImplicit() && constraintTypes.IsEmpty)
            {
                diagnostics.Add(ErrorCode.ERR_NoConstraintsOnImplicitParam, location, Name);
            }

            foreach (var constraintType in constraintTypes)
            {
                // @t-mawind
                //   Concept constraints can be used if and only if this
                //   parameter has 'implicit' set. 
                if (constraintType.IsConceptType() && !WasDeclaredImplicit())
                {
                    diagnostics.Add(ErrorCode.ERR_ConceptConstraintOnNonImplicitParam, location, Name, constraintType.Name);
                }
                if (!constraintType.IsConceptType() && WasDeclaredImplicit())
                {
                    diagnostics.Add(ErrorCode.ERR_NonConceptConstraintOnImplicitParam, location, Name, constraintType.Name);
                }

                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                constraintType.AddUseSiteDiagnostics(ref useSiteDiagnostics);

                if (!diagnostics.Add(location, useSiteDiagnostics))
                {
                    constraintType.CheckAllConstraints(conversions, location, diagnostics);
                }
            }
        }

        private NamedTypeSymbol GetDefaultBaseType()
        {
            return this.ContainingAssembly.GetSpecialType(SpecialType.System_Object);
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var incompletePart = _state.NextIncompletePart;
                switch (incompletePart)
                {
                    case CompletionPart.Attributes:
                        GetAttributes();
                        break;

                    case CompletionPart.TypeParameterConstraints:
                        var constraintTypes = this.ConstraintTypesNoUseSiteDiagnostics;

                        // Nested type parameter references might not be valid in error scenarios.
                        //Debug.Assert(this.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(this.ConstraintTypes));
                        //Debug.Assert(this.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(ImmutableArray<TypeSymbol>.CreateFrom(this.Interfaces)));
                        Debug.Assert(this.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(this.EffectiveBaseClassNoUseSiteDiagnostics));
                        Debug.Assert(this.ContainingSymbol.IsContainingSymbolOfAllTypeParameters(this.DeducedBaseTypeNoUseSiteDiagnostics));
                        break;

                    case CompletionPart.None:
                        return;

                    default:
                        // any other values are completion parts intended for other kinds of symbols
                        _state.NotePartComplete(CompletionPart.All & ~CompletionPart.TypeParameterSymbolAll);
                        break;
                }

                _state.SpinWaitComplete(incompletePart, cancellationToken);
            }
        }

        /// <summary>
        /// Decides whether this type parameter is a concept witness.
        /// <para>
        /// Source type parameters are concept witnesses if, and only if,
        /// they were declared with the implicit keyword.  This is because
        /// we raise errors if the parameter was declared with the keyword
        /// and not a concept witness as per the usual test, and vice
        /// versa.
        /// </para>
        /// <para>
        /// We override this to prevent an infinite loop in constraint
        /// resolution.
        /// </para>
        /// </summary>
        internal override bool IsConceptWitness => WasDeclaredImplicit();

        internal sealed override bool IsAssociatedType
        {
            get
            {
                // Usually, types are associated types if they have
                // [AssociatedType]. @t-mawind
                foreach (var attribute in GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.AssociatedTypeAttribute)) return true;
                }

                return false;
            }
        }
    }

    internal sealed class SourceTypeParameterSymbol : SourceTypeParameterSymbolBase
    {
        private readonly SourceNamedTypeSymbol _owner;
        private readonly VarianceKind _varianceKind;

        public SourceTypeParameterSymbol(SourceNamedTypeSymbol owner, string name, int ordinal, VarianceKind varianceKind, ImmutableArray<Location> locations, ImmutableArray<SyntaxReference> syntaxRefs)
            : base(name, ordinal, locations, syntaxRefs)
        {
            _owner = owner;
            _varianceKind = varianceKind;
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Type;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _owner; }
        }

        public override VarianceKind Variance
        {
            get { return _varianceKind; }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.Constructor) != 0;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.ValueType) != 0;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.ReferenceType) != 0;
            }
        }

        protected override ImmutableArray<TypeParameterSymbol> ContainerTypeParameters
        {
            get { return _owner.TypeParameters; }
        }

        protected override TypeParameterBounds ResolveBounds(ConsList<TypeParameterSymbol> inProgress, DiagnosticBag diagnostics)
        {
            var constraintTypes = _owner.GetTypeParameterConstraintTypes(this.Ordinal);
            return this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, false, this.DeclaringCompilation, diagnostics);
        }

        private TypeParameterConstraintKind GetDeclaredConstraints()
        {
            // @t-mawind
            //   Add in 'struct' if this is a concept witness.
            //   This used to be done through a synthesised symbol, but now we
            //   have more explicit syntax it has to be done here--ugly!
            var ownerConstraints = _owner.GetTypeParameterConstraints(this.Ordinal);
            return IsConceptWitness ? (ownerConstraints | TypeParameterConstraintKind.ValueType) : ownerConstraints;
        }
    }

    internal sealed class SourceMethodTypeParameterSymbol : SourceTypeParameterSymbolBase
    {
        private readonly SourceMethodSymbol _owner;

        public SourceMethodTypeParameterSymbol(SourceMethodSymbol owner, string name, int ordinal, ImmutableArray<Location> locations, ImmutableArray<SyntaxReference> syntaxRefs)
            : base(name, ordinal, locations, syntaxRefs)
        {
            _owner = owner;
        }

        internal override void AddDeclarationDiagnostics(DiagnosticBag diagnostics)
            => _owner.AddDeclarationDiagnostics(diagnostics);

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Method;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _owner; }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.Constructor) != 0;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.ValueType) != 0;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                var constraints = this.GetDeclaredConstraints();
                return (constraints & TypeParameterConstraintKind.ReferenceType) != 0;
            }
        }

        protected override ImmutableArray<TypeParameterSymbol> ContainerTypeParameters
        {
            get { return _owner.TypeParameters; }
        }

        protected override TypeParameterBounds ResolveBounds(ConsList<TypeParameterSymbol> inProgress, DiagnosticBag diagnostics)
        {
            var constraintClauses = _owner.TypeParameterConstraintClauses;
            var constraintTypes = constraintClauses.IsEmpty
                ? ImmutableArray<TypeSymbol>.Empty
                : constraintClauses[Ordinal]?.ConstraintTypes ?? ImmutableArray<TypeSymbol>.Empty;
            return this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, false, this.DeclaringCompilation, diagnostics);
        }

        private TypeParameterConstraintKind GetDeclaredConstraints()
        {
            var constraintClauses = _owner.TypeParameterConstraintClauses;
            var ownerConstraints = constraintClauses.IsEmpty
                ? TypeParameterConstraintKind.None
                : constraintClauses[Ordinal]?.Constraints ?? TypeParameterConstraintKind.None;

            // @t-mawind
            //   See above type's GetDeclaredConstraints()--same reasoning.
            return IsConceptWitness ? (ownerConstraints | TypeParameterConstraintKind.ValueType) : ownerConstraints;
        }
    }

    /// <summary>
    /// A map shared by all type parameters for an overriding method or a method
    /// that explicitly implements an interface. The map caches the overridden method
    /// and a type map from overridden type parameters to overriding type parameters.
    /// </summary>
    internal abstract class OverriddenMethodTypeParameterMapBase
    {
        // Method representing overriding or explicit implementation.
        private readonly SourceOrdinaryMethodSymbol _overridingMethod;

        // Type map shared by all type parameters for this explicit implementation.
        private TypeMap _lazyTypeMap;

        // Overridden or explicitly implemented method. May be null in error cases.
        private MethodSymbol _lazyOverriddenMethod = ErrorMethodSymbol.UnknownMethod;

        protected OverriddenMethodTypeParameterMapBase(SourceOrdinaryMethodSymbol overridingMethod)
        {
            _overridingMethod = overridingMethod;
        }

        public SourceOrdinaryMethodSymbol OverridingMethod
        {
            get { return _overridingMethod; }
        }

        public TypeParameterSymbol GetOverriddenTypeParameter(int ordinal)
        {
            var overriddenMethod = this.OverriddenMethod;
            return ((object)overriddenMethod != null) ? overriddenMethod.TypeParameters[ordinal] : null;
        }

        public TypeMap TypeMap
        {
            get
            {
                if (_lazyTypeMap == null)
                {
                    var overriddenMethod = this.OverriddenMethod;
                    if ((object)overriddenMethod != null)
                    {
                        var overriddenTypeParameters = overriddenMethod.TypeParameters;
                        var overridingTypeParameters = _overridingMethod.TypeParameters;

                        Debug.Assert(overriddenTypeParameters.Length == overridingTypeParameters.Length);

                        var typeMap = new TypeMap(overriddenTypeParameters, overridingTypeParameters, allowAlpha: true);
                        Interlocked.CompareExchange(ref _lazyTypeMap, typeMap, null);
                    }
                }

                return _lazyTypeMap;
            }
        }

        private MethodSymbol OverriddenMethod
        {
            get
            {
                if (ReferenceEquals(_lazyOverriddenMethod, ErrorMethodSymbol.UnknownMethod))
                {
                    Interlocked.CompareExchange(ref _lazyOverriddenMethod, this.GetOverriddenMethod(_overridingMethod), ErrorMethodSymbol.UnknownMethod);
                }
                return _lazyOverriddenMethod;
            }
        }

        protected abstract MethodSymbol GetOverriddenMethod(SourceOrdinaryMethodSymbol overridingMethod);
    }

    internal sealed class OverriddenMethodTypeParameterMap : OverriddenMethodTypeParameterMapBase
    {
        public OverriddenMethodTypeParameterMap(SourceOrdinaryMethodSymbol overridingMethod)
            : base(overridingMethod)
        {
            Debug.Assert(overridingMethod.IsOverride);
        }

        protected override MethodSymbol GetOverriddenMethod(SourceOrdinaryMethodSymbol overridingMethod)
        {
            MethodSymbol method = overridingMethod;
            Debug.Assert(method.IsOverride);
            do
            {
                method = method.OverriddenMethod;
            } while (((object)method != null) && method.IsOverride);
            // OverriddenMethod may be null in error situations.
            return method;
        }
    }

    internal sealed class ExplicitInterfaceMethodTypeParameterMap : OverriddenMethodTypeParameterMapBase
    {
        public ExplicitInterfaceMethodTypeParameterMap(SourceOrdinaryMethodSymbol implementationMethod)
            : base(implementationMethod)
        {
            Debug.Assert(implementationMethod.IsExplicitInterfaceImplementation);
        }

        protected override MethodSymbol GetOverriddenMethod(SourceOrdinaryMethodSymbol overridingMethod)
        {
            var explicitImplementations = overridingMethod.ExplicitInterfaceImplementations;
            Debug.Assert(explicitImplementations.Length <= 1);

            // ExplicitInterfaceImplementations may be empty in error situations.
            return (explicitImplementations.Length > 0) ? explicitImplementations[0] : null;
        }
    }

    /// <summary>
    /// A type parameter for a method that either overrides a base
    /// type method or explicitly implements an interface method.
    /// </summary>
    /// <remarks>
    /// Exists to copy constraints from the corresponding type parameter of an overridden method.
    /// </remarks>
    internal sealed class SourceOverridingMethodTypeParameterSymbol : SourceTypeParameterSymbolBase
    {
        private readonly OverriddenMethodTypeParameterMapBase _map;

        public SourceOverridingMethodTypeParameterSymbol(OverriddenMethodTypeParameterMapBase map, string name, int ordinal, ImmutableArray<Location> locations, ImmutableArray<SyntaxReference> syntaxRefs)
            : base(name, ordinal, locations, syntaxRefs)
        {
            _map = map;
        }

        public SourceOrdinaryMethodSymbol Owner
        {
            get { return _map.OverridingMethod; }
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return TypeParameterKind.Method;
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.Owner; }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                var typeParameter = this.OverriddenTypeParameter;
                return ((object)typeParameter != null) && typeParameter.HasConstructorConstraint;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                var typeParameter = this.OverriddenTypeParameter;
                return ((object)typeParameter != null) && typeParameter.HasValueTypeConstraint;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                var typeParameter = this.OverriddenTypeParameter;
                return ((object)typeParameter != null) && typeParameter.HasReferenceTypeConstraint;
            }
        }

        protected override ImmutableArray<TypeParameterSymbol> ContainerTypeParameters
        {
            get { return this.Owner.TypeParameters; }
        }

        protected override TypeParameterBounds ResolveBounds(ConsList<TypeParameterSymbol> inProgress, DiagnosticBag diagnostics)
        {
            var typeParameter = this.OverriddenTypeParameter;
            if ((object)typeParameter == null)
            {
                return null;
            }

            var map = _map.TypeMap;
            Debug.Assert(map != null);

            var constraintTypes = map.SubstituteTypesWithoutModifiers(typeParameter.ConstraintTypesNoUseSiteDiagnostics);
            return this.ResolveBounds(this.ContainingAssembly.CorLibrary, inProgress.Prepend(this), constraintTypes, true, this.DeclaringCompilation, diagnostics);
        }

        /// <summary>
        /// The type parameter to use for determining constraints. If there is a base
        /// method that the owner method is overriding, the corresponding type
        /// parameter on that method is used. Otherwise, the result is null.
        /// </summary>
        private TypeParameterSymbol OverriddenTypeParameter
        {
            get
            {
                return _map.GetOverriddenTypeParameter(this.Ordinal);
            }
        }
    }
}
