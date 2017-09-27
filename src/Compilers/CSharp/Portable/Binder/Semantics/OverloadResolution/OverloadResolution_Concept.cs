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
        /// parameter list in the concepts in scope at this stage.
        /// </summary>
        /// <param name="name">
        /// The special name of the operator to find.
        /// </param>
        /// <param name="args">
        /// The arguments being supplied to the operator.
        /// </param>
        /// <param name="useSiteDiagnostics">
        /// The set of diagnostics to populate with any use-site diagnostics
        /// coming from this lookup.
        /// </param>
        /// <returns>
        /// An array of possible matches for the given operator.
        /// </returns>
        private ImmutableArray<MethodSymbol> GetConceptOperators(string name, ImmutableArray<BoundExpression> args, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
                        if (candidate == null)
                        {
                            continue;
                        }
                        if (candidate.Kind != SymbolKind.Method)
                        {
                            continue;
                        }
                        var method = (MethodSymbol)candidate;
                        if (method.MethodKind != MethodKind.UserDefinedOperator)
                        {
                            continue;
                        }
                        if (method.ParameterCount != args.Length)
                        {
                            continue;
                        }

                        // @MattWindsor91 (Concept-C# 2017)
                        //
                        // Unlike normal operator overloads, concept operators
                        // will have missing type parameters: at the very least, the
                        // witness parameter telling us which concept instance to
                        // call into will be unknown.
                        //
                        // In this prototype, we just call a full round of method
                        // type inference, and ignore the method if it doesn't infer.
                        //
                        // This probably doesn't handle nullability correctly.
                        HashSet<DiagnosticInfo> ignore = null;
                        // As with MethodCompiler.BindMethodBody, we need to
                        // pull in the witnesses of a default struct into scope.
                        var mtr = MethodTypeInferrer.Infer(_binder, method.TypeParameters, method.ContainingType, method.ParameterTypes, method.ParameterRefKinds, args, ref ignore);
                        if (!mtr.Success)
                        {
                            continue;
                        }

                        haveCandidates = true;
                        if (method is SynthesizedImplicitConceptMethodSymbol imethod)
                        {
                            builder.Add(imethod.ConstructAndRetarget(mtr.InferredTypeArguments));
                        }
                        else
                        {
                            builder.Add(method.Construct(mtr.InferredTypeArguments));
                        }
                    }

                    // We're currently doing this fairly similarly to the way
                    // normal method lookup works: the moment any scope gives
                    // us at least one possible operator, use only that scope's
                    // results.  I'm not sure whether this is correct, but at
                    // least it's consistent.
                    if (haveCandidates)
                    {
                        return builder.ToImmutableAndFree();
                    }
                }
            }

            // At this stage, we haven't seen _any_ operators.
            builder.Free();
            return ImmutableArray<MethodSymbol>.Empty;
        }
    }
}
