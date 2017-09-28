// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Symbol representing an implicit call into a concept method, which
    /// rewrites the call as if it is a standalone call needing concept
    /// inference.
    /// </summary>
    /// <remarks>
    /// As with many of the Concept-C# synthesized symbols, this is
    /// mainly a placeholder until something more principled can be written.
    /// </remarks>
    internal sealed class SynthesizedImplicitConceptMethodSymbol : WrappedMethodSymbol
    {
        /// <summary>
        /// The arity of the symbol, including its adopted type parameters.
        /// </summary>
        private int _arity;

        /// <summary>
        /// The concept into which we were calling.
        /// </summary>
        private NamedTypeSymbol _concept;

        /// <summary>
        /// The concept method to wrap.
        /// </summary>
        private MethodSymbol _method;

        /// <summary>
        /// The type parameters of the symbol, including those of the
        /// receiving concept and the concept witness itself.
        /// </summary>
        private ImmutableArray<TypeParameterSymbol> _typeParameters;

        /// <summary>
        /// The type arguments of the symbol, including those of the
        /// receiving concept and the concept witness itself.
        /// </summary>
        private ImmutableArray<TypeSymbol> _typeArguments;

        /// <summary>
        /// Constructs a new <see cref="SynthesizedImplicitConceptMethodSymbol"/>.
        /// </summary>
        /// <param name="method">
        /// The concept method to wrap.
        /// </param>
        internal SynthesizedImplicitConceptMethodSymbol(MethodSymbol method)
            : base()
        {
            Debug.Assert(method.ReceiverType != null, "implicit concept method must have a receiver");
            Debug.Assert(method.ReceiverType.IsConceptType(), "must wrap a method taken from a concept");
            Debug.Assert(method.ReceiverType.Kind == SymbolKind.NamedType, "concept receiver should be a named type");
            _concept = (NamedTypeSymbol)method.ReceiverType;

            _method = method;

            var paramsB = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            paramsB.AddRange(_method.TypeParameters);
            paramsB.AddRange(_concept.TypeParameters);
            // To make sure that any concept inference on this method
            // pulls down the correct concept, we also add a concept
            // witness as an extra, synthesised, type parameter.
            var witnessOrdinal = _method.Arity + _concept.Arity;
            var witness =
                new SynthesizedWitnessParameterSymbol(
                    GeneratedNames.MakeAnonymousTypeParameterName("witness"),
                    Location.None,
                    witnessOrdinal,
                    _method,
                    _ => ImmutableArray.Create((TypeSymbol)_concept),
                    _ => TypeParameterConstraintKind.ValueType);
            paramsB.Add(witness);
            _typeParameters = paramsB.ToImmutableAndFree();

            var argsB = ArrayBuilder<TypeSymbol>.GetInstance();
            argsB.AddRange(_method.TypeArguments);
            argsB.AddRange(_concept.TypeArguments);
            argsB.Add(witness);
            _typeArguments = argsB.ToImmutableAndFree();

            _arity = _typeArguments.Length;
        }

        /// <summary>
        /// Constructs this implicit method, undoing the encoding of the
        /// concept parameters and instance in the process and retrieving
        /// a synthesised witness symbol.
        /// </summary>
        /// <param name="typeArguments">
        /// The full set of type arguments supplied for constructing
        /// this symbol, which map in order to the method parameters,
        /// concept parameters, and concept instance respectively.
        /// </param>
        /// <returns>
        /// A symbol representing the original implicit method, but with
        /// the concept-level and method-level inferences applied, and the
        /// inferred witness attached for later use in lowering.
        /// </returns>
        internal MethodSymbol ConstructAndRetarget(ImmutableArray<TypeSymbol> typeArguments)
        {
            Debug.Assert(!typeArguments.IsDefaultOrEmpty, "expected a valid type argument array to construct with");
            Debug.Assert(typeArguments.Length == Arity, "arity mismatch on type arguments");

            // As per the constructor, the type arguments should contain:
            // - All method type arguments (to send straight to construction);
            // - All concept type arguments (to ignore: we already have the
            //   concept instance fully constructed);
            // - The concept instance (to use as a receiver);

            MethodSymbol substituted = SubstituteForConstructAndRetarget(typeArguments);
            MethodSymbol constructed = ConstructForConstructAndRetarget(typeArguments, substituted);

            var instance = typeArguments[Arity - 1];
            Debug.Assert(instance != null, "type inference should have given us a non-null instance");
            Debug.Assert(instance.IsInstanceType() || instance.IsConceptWitness, "type inference should have made the last argument a concept instance");

            return new SynthesizedWitnessMethodSymbol(constructed, instance);
        }

        /// <summary>
        /// Performs the substitution (type-level) part of a
        /// 'construct and retarget' on an implicit concept method.
        /// </summary>
        /// <param name="typeArguments">
        /// The full set of type arguments supplied for constructing
        /// this symbol, which map in order to the method parameters,
        /// concept parameters, and concept instance respectively.
        /// </param>
        /// <returns>
        /// The original implicit method, with the new concept type arguments
        /// from <paramref name="typeArguments"/> substituted for the original
        /// concept parameters.
        /// If the concept had no type parameters, or the type arguments are
        /// equal to the type parameters, this is a no-operation.
        /// </returns>
        private MethodSymbol SubstituteForConstructAndRetarget(ImmutableArray<TypeSymbol> typeArguments)
        {
            if (_concept.Arity == 0)
            {
                return UnderlyingMethod;
            }
            // Backform what the constructed form of the concept will be after
            // applying the information from the inference.
            var conceptTypeArgumentsB = ArrayBuilder<TypeSymbol>.GetInstance();
            for (var j = UnderlyingMethod.Arity; j < Arity - 1; j++)
            {
                conceptTypeArgumentsB.Add(typeArguments[j]);
            }
            var conceptTypeArguments = conceptTypeArgumentsB.ToImmutableAndFree();
            var constructedConcept = _concept.Construct(conceptTypeArguments);
            if (constructedConcept == _concept)
            {
                // Construction was a no-op, so substituting would be an error.
                return UnderlyingMethod;
            }
            return new SubstitutedMethodSymbol(constructedConcept, UnderlyingMethod);
        }

        /// <summary>
        /// Performs the construction (method-level) part of a
        /// 'construct and retarget' on an implicit concept method.
        /// </summary>
        /// <param name="typeArguments">
        /// The full set of type arguments supplied for constructing
        /// this symbol, which map in order to the method parameters,
        /// concept parameters, and concept instance respectively.
        /// </param>
        /// <param name="substituted">
        /// The pre-substituted method, which will now be constructed.
        /// </param>
        /// <returns>
        /// The method <paramref name="substituted"/>, but with the new method
        /// type arguments substituted for the original method parameters.
        /// If the method had no type parameters, this is a no-operation.
        /// </returns>
        private MethodSymbol ConstructForConstructAndRetarget(ImmutableArray<TypeSymbol> typeArguments, MethodSymbol substituted)
        {
            if (UnderlyingMethod.Arity == 0)
            {
                return substituted;
            }

            var methodTypeArgumentsB = ArrayBuilder<TypeSymbol>.GetInstance();
            for (var i = 0; i < UnderlyingMethod.Arity; i++)
            {
                methodTypeArgumentsB.Add(typeArguments[i]);
            }
            var methodTypeArguments = methodTypeArgumentsB.ToImmutableAndFree();

            return new ConstructedMethodSymbol(substituted, methodTypeArguments);
        }

        public override int Arity => _arity;

        public override MethodSymbol UnderlyingMethod => _method;

        public override TypeSymbol ReceiverType => null;

        // @MattWindsor91 (Concept-C# 2017)
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

        public override ImmutableArray<TypeSymbol> TypeArguments => _typeArguments;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;

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

    /*

    internal sealed class RetargetedConstructedMethodSymbol : ConstructedMethodSymbol
    {
        private NamedTypeSymbol _receiver;

        private NamedTypeSymbol _parent;

        private ImmutableArray<ParameterSymbol> _parameters;

        internal RetargetedConstructedMethodSymbol(MethodSymbol constructedFrom, ImmutableArray<TypeSymbol> typeArguments, NamedTypeSymbol newReceiver)
            : base(constructedFrom, typeArguments, newReceiver)
        {
            _receiver = newReceiver;

            for 
        }

        public override TypeSymbol ReceiverType => _receiver;

        
    }*/
}
