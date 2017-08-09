// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class OverloadResolution
    {
        /// <summary>
        /// Tries to find candidate operators for a given operator name and
        /// number of parameters in this scope's witnesses.
        /// </summary>
        /// <param name="name">
        /// The special name of the operator to find.
        /// </param>
        /// <param name="numParams">
        /// The number of parameters the operator takes: 1 for unary,
        /// 2 for binary.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// The set of diagnostics to populate with any use-site diagnostics
        /// coming from this lookup.
        /// </param>
        /// <returns>
        /// An array of possible matches for the given operator.
        /// </returns>
        private ImmutableArray<MethodSymbol> GetWitnessOperators(string name, int numParams, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            var builder = ArrayBuilder<MethodSymbol>.GetInstance();
            var result = LookupResult.GetInstance();

            // @t-mawind
            //   This is a fairly crude, and potentially very incorrect, method
            //   of finding overloads--can it be improved?
            for (var scope = _binder; scope != null; scope = scope.Next)
            {
                scope.LookupConceptMethodsInSingleBinder(result, name, 0, null, LookupOptions.AllMethodsOnArityZero | LookupOptions.AllowSpecialMethods, _binder, true, ref useSiteDiagnostics);
                if (result.IsMultiViable)
                {
                    var haveCandidates = false;

                    foreach (var candidate in result.Symbols)
                    {
                        var meth = candidate as MethodSymbol;
                        if (meth == null) continue;
                        if (meth.MethodKind != MethodKind.UserDefinedOperator) continue;
                        if (meth.ParameterCount != numParams) continue;

                        haveCandidates = true;
                        builder.Add(meth);
                    }
                    
                    // We're currently doing this fairly similarly to the way
                    // normal method lookup works: the moment any scope gives
                    // us at least one possible operator, use only that scope's
                    // results.  I'm not sure whether this is correct, but at
                    // least it's consistent.
                    if (haveCandidates) return builder.ToImmutableAndFree();
                }
            }

            // At this stage, we haven't seen _any_ operators.
            builder.Free();
            return ImmutableArray<MethodSymbol>.Empty;
        }
    }
}
