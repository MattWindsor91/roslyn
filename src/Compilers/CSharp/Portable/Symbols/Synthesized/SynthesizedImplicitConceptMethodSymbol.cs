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
        private NamedTypeSymbol _originalReceiver;

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
            Debug.Assert(method.ReceiverType.IsConceptOrStandaloneInstanceType(),
                "must wrap a method taken from a concept or standalone instance");
            Debug.Assert(method.ReceiverType.Kind == SymbolKind.NamedType, "concept receiver should be a named type");
            _originalReceiver = (NamedTypeSymbol)method.ReceiverType;

            _method = method;

            var paramsB = ArrayBuilder<TypeParameterSymbol>.GetInstance();
            paramsB.AddRange(_method.TypeParameters);
            paramsB.AddRange(_originalReceiver.TypeParameters);

            var argsB = ArrayBuilder<TypeSymbol>.GetInstance();
            argsB.AddRange(_method.TypeArguments);
            argsB.AddRange(_originalReceiver.TypeArguments);

            if (_originalReceiver.IsConcept)
            {
                // To make sure that any concept inference on this method
                // pulls down the correct concept, we also add a concept
                // witness as an extra, synthesised, type parameter.
                var witnessOrdinal = _method.Arity + _originalReceiver.Arity;
                var witness =
                    new SynthesizedWitnessParameterSymbol(
                        GeneratedNames.WitnessTypeParameterName(),
                        Location.None,
                        witnessOrdinal,
                        _method,
                        _ => ImmutableArray.Create((TypeSymbol)_originalReceiver),
                        _ => TypeParameterConstraintKind.ValueType);

                paramsB.Add(witness);
                argsB.Add(witness);
            }

            _typeParameters = paramsB.ToImmutableAndFree();
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

            (var methodArgs, var recvArgs) = PartitionTypeArgs(typeArguments);
            var constructedReceiver = _originalReceiver.ConstructIfGeneric(recvArgs);

            MethodSymbol substituted = SubstituteForConstructAndRetarget(constructedReceiver);
            MethodSymbol constructed = ConstructForConstructAndRetarget(methodArgs, substituted);

            var instance = _originalReceiver.IsConcept ? typeArguments[Arity - 1] : constructedReceiver;
            Debug.Assert(instance != null, "type inference should have given us a non-null instance");
            Debug.Assert(instance.IsInstanceType() || instance.IsConceptWitness, "type inference should have made the last argument a concept instance");

            return new SynthesizedWitnessMethodSymbol(constructed, instance);
        }

        private (ImmutableArray<TypeSymbol> methodArgs, ImmutableArray<TypeWithModifiers> recvArgs) PartitionTypeArgs(ImmutableArray<TypeSymbol> typeArguments)
        {
            // As per the constructor, the type arguments should contain:
            // - All method type arguments (to send straight to construction);
            // - All concept/standalone instance type arguments; 
            // - If we're on a concept, the concept instance (to use as a receiver).
            //   Otherwise, we're on a standalone instance and just use that as
            //   the receiver (after constructing it with its new arguments).
            var methodArgsB = ArrayBuilder<TypeSymbol>.GetInstance();
            for (int i = 0; i < UnderlyingMethod.Arity; ++i)
            {
                methodArgsB.Add(typeArguments[i]);
            }
            var methodArgs = methodArgsB.ToImmutableAndFree();

            var recvArgsB = ArrayBuilder<TypeWithModifiers>.GetInstance();
            for (int i = UnderlyingMethod.Arity; i < UnderlyingMethod.Arity + _originalReceiver.Arity; ++i)
            {
                recvArgsB.Add(new TypeWithModifiers(typeArguments[i]));
            }
            var recvArgs = recvArgsB.ToImmutableAndFree();

            return (methodArgs, recvArgs);
        }

        /// <summary>
        /// Performs the substitution (type-level) part of a
        /// 'construct and retarget' on an implicit concept method.
        /// </summary>
        /// <param name="constructedReceiver">
        /// The new, constructed receiver (concept or standalone instance).
        /// </param>
        /// <returns>
        /// The original implicit method, with the new receiver substituted for
        /// the old one.
        /// If the receiver hadn't changed under construction, this is a no-operation.
        /// </returns>
        private MethodSymbol SubstituteForConstructAndRetarget(NamedTypeSymbol constructedReceiver)
        {
            if (_originalReceiver == constructedReceiver)
            {
                return UnderlyingMethod;
            }
            return new SubstitutedMethodSymbol(constructedReceiver, UnderlyingMethod);
        }

        /// <summary>
        /// Performs the construction (method-level) part of a
        /// 'construct and retarget' on an implicit concept method.
        /// </summary>
        /// <param name="methodTypeArguments">
        /// The new set of method type arguments.
        /// </param>
        /// <param name="substituted">
        /// The pre-substituted method, which will now be constructed.
        /// </param>
        /// <returns>
        /// The method <paramref name="substituted"/>, but with the new method
        /// type arguments substituted for the original method parameters.
        /// If the method had no type parameters, this is a no-operation.
        /// </returns>
        private MethodSymbol ConstructForConstructAndRetarget(ImmutableArray<TypeSymbol> methodTypeArguments, MethodSymbol substituted)
        {
            if (UnderlyingMethod.Arity == 0)
            {
                return substituted;
            }
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
}
