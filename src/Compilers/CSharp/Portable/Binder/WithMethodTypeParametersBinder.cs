// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A binder that places method type parameters in scope.
    /// </summary>
    internal sealed class WithMethodTypeParametersBinder : WithTypeParametersBinder
    {
        private readonly MethodSymbol _methodSymbol;
        private MultiDictionary<string, TypeParameterSymbol> _lazyTypeParameterMap;

        internal WithMethodTypeParametersBinder(MethodSymbol methodSymbol, Binder next)
            : base(next)
        {
            _methodSymbol = methodSymbol;
        }

        internal override Symbol ContainingMemberOrLambda
        {
            get
            {
                return _methodSymbol;
            }
        }

        protected override MultiDictionary<string, TypeParameterSymbol> TypeParameterMap
        {
            get
            {
                if (_lazyTypeParameterMap == null)
                {
                    var result = new MultiDictionary<string, TypeParameterSymbol>();
                    foreach (var typeParameter in _methodSymbol.TypeParameters)
                    {
                        result.Add(typeParameter.Name, typeParameter);
                    }

                    Interlocked.CompareExchange(ref _lazyTypeParameterMap, result, null);
                }

                return _lazyTypeParameterMap;
            }
        }

        protected override LookupOptions LookupMask
        {
            get
            {
                return LookupOptions.NamespaceAliasesOnly | LookupOptions.MustNotBeMethodTypeParameter;
            }
        }

        protected override void AddLookupSymbolsInfoInSingleBinder(LookupSymbolsInfo result, LookupOptions options, Binder originalBinder)
        {
            if (CanConsiderTypeParameters(options))
            {
                foreach (var parameter in _methodSymbol.TypeParameters)
                {
                    if (originalBinder.CanAddLookupSymbolInfo(parameter, options, null))
                    {
                        result.AddSymbol(parameter, parameter.Name, 0);
                    }
                }
            }
        }

        internal override void GetConceptInstances(bool onlyExplicitWitnesses, ArrayBuilder<TypeSymbol> instances, Binder originalBinder, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            foreach (var parameter in _methodSymbol.TypeParameters)
            {
                if (parameter.IsConceptWitness) instances.Add(parameter);
            }
        }

        internal override void GetFixedTypeParameters(ArrayBuilder<TypeParameterSymbol> fixedTypeParams)
        {
            foreach (var parameter in _methodSymbol.TypeParameters) fixedTypeParams.Add(parameter);
        }
    }
}
