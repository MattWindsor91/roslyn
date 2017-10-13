// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A synthesized concept instance method that, when generated,
    /// calls into a corresponding static operator on the type of its first
    /// argument.
    /// </summary>
    internal sealed class SynthesizedOperatorShimMethod : SynthesizedInstanceShimMethod
    {
        public SynthesizedOperatorShimMethod(MethodSymbol conceptMethod, NamedTypeSymbol implementingType)
            : base(conceptMethod, implementingType)
        {
            Debug.Assert(conceptMethod.IsOperator(),
                "Trying to create operator shim for a non-operator");
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "Trying to create operator shim for a 0-argument operator (?)");
        }

        protected override BoundExpression GenerateReceiver(SyntheticBoundNodeFactory f)
        {
            // This is calling into a non-concept operator on the first
            // parameter type.  To make this shim, we _should_ have checked
            // that this type actually has such an operator.
            return f.Type(ImplementingMethod.ParameterTypes[0]);
        }

        protected override ImmutableArray<LocalSymbol> GenerateLocals(SyntheticBoundNodeFactory f, BoundExpression receiver)
        {
            return ImmutableArray<LocalSymbol>.Empty;
        }

        protected override ImmutableArray<BoundExpression> GenerateInnerCallArguments(SyntheticBoundNodeFactory f)
        {
            var argumentsB = ArrayBuilder<BoundExpression>.GetInstance();
            foreach (var p in ImplementingMethod.Parameters)
            {
                argumentsB.Add(f.Parameter(p));
            }
            return argumentsB.ToImmutableAndFree();
        }

        protected override BoundExpression GenerateCall(SyntheticBoundNodeFactory f, BoundExpression receiver, ImmutableArray<BoundExpression> arguments, DiagnosticBag diagnostics)
        {
            // For user-defined operators, the normal shim call approach works
            // fine.  However, we also want to permit shim calls into types
            // with builtin operators, for which we need to do an actual
            // lookup as to which operator we're defining, and a quick
            // jump into part of the operator overload resolution code to see
            // if we're actually in one of those types.
            //
            // This may be incomplete, but is probably(??) sound.
            if (ImplementingMethod.OriginalDefinition is SourceUserDefinedOperatorSymbol op)
            {
                Debug.Assert(receiver.Kind == BoundKind.TypeExpression,
                    "receiver of an operator should always be a type");
                var rectype = (BoundTypeExpression)receiver;

                var opdecl = op.GetSyntax();
                Debug.Assert(opdecl != null, "should have operator syntax here");
                var opsyn = opdecl.OperatorToken;
                var opkind = opsyn.Kind();

                var binder = DeclaringCompilation.GetBinder(ImplementingMethod.GetNonNullSyntaxNode()).WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.InShim, this);
                var ovr = new OverloadResolution(binder);
                var ignore = new HashSet<DiagnosticInfo>();

                if (arguments.Length == 1)
                {
                    (var usuccess, var ukind) = TryGetUnaryOperatorKind(opsyn.Kind());
                    if (usuccess)
                    {
                        var result = UnaryOperatorOverloadResolutionResult.GetInstance();
                        ovr.UnaryOperatorOverloadResolution(ukind, arguments[0], result, ref ignore);
                        if (result.SingleValid())
                        {
                            var bsig = result.Best.Signature;
                            if (bsig.Method == null)
                            {
                                return f.Unary(bsig.Kind, bsig.ReturnType, arguments[0]);
                            }
                        }
                    }
                }
                else if (arguments.Length == 2)
                {
                    (var bsuccess, var bkind) = TryGetBinaryOperatorKind(opsyn.Kind());
                    if (bsuccess)
                    {
                        var result = BinaryOperatorOverloadResolutionResult.GetInstance();
                        ovr.BinaryOperatorOverloadResolution(bkind, arguments[0], arguments[1], result, ref ignore);
                        if (result.SingleValid())
                        {
                            var bsig = result.Best.Signature;
                            if (bsig.Method == null)
                            {
                                return f.Binary(bsig.Kind, bsig.ReturnType, arguments[0], arguments[1]);
                            }
                        }
                    }
                }
            }

            return base.GenerateCall(f, receiver, arguments, diagnostics);
        }

        private (bool success, UnaryOperatorKind kind) TryGetUnaryOperatorKind(SyntaxKind kind)
        {
            // TODO(@MattWindsor91): this is mostly copied from
            //     Binder_Operators#SyntaxKindToBinaryOperatorKind.
            //     Ideally it should be deduplicated.
            switch (kind)
            {
                // We don't yet allow -- and ++, because it's ambiguous
                // as to whether it should be in prefix or postfix
                // position.
                case SyntaxKind.PlusToken: return (true, UnaryOperatorKind.UnaryPlus);
                case SyntaxKind.MinusToken: return (true, UnaryOperatorKind.UnaryMinus);
                case SyntaxKind.ExclamationToken: return (true, UnaryOperatorKind.LogicalNegation);
                case SyntaxKind.TildeToken: return (true, UnaryOperatorKind.BitwiseComplement);
                default: return (false, default);
            }
        }

        private (bool success, BinaryOperatorKind kind) TryGetBinaryOperatorKind(SyntaxKind kind)
        {
            // TODO(@MattWindsor91): this is mostly copied from
            //     Binder_Operators#SyntaxKindToBinaryOperatorKind.
            //     Ideally it should be deduplicated.
            switch (kind)
            {
                // Omitting operators not overridable
                case SyntaxKind.AsteriskToken: return (true, BinaryOperatorKind.Multiplication);
                case SyntaxKind.SlashToken: return (true, BinaryOperatorKind.Division);
                case SyntaxKind.PercentToken: return (true, BinaryOperatorKind.Remainder);
                case SyntaxKind.PlusToken: return (true, BinaryOperatorKind.Addition);
                case SyntaxKind.MinusToken: return (true, BinaryOperatorKind.Subtraction);
                case SyntaxKind.GreaterThanGreaterThanToken: return (true, BinaryOperatorKind.RightShift);
                case SyntaxKind.LessThanLessThanToken: return (true, BinaryOperatorKind.LeftShift);
                case SyntaxKind.EqualsEqualsToken: return (true, BinaryOperatorKind.Equal);
                case SyntaxKind.ExclamationEqualsToken: return (true, BinaryOperatorKind.NotEqual);
                case SyntaxKind.GreaterThanToken: return (true, BinaryOperatorKind.GreaterThan);
                case SyntaxKind.LessThanToken: return (true, BinaryOperatorKind.LessThan);
                case SyntaxKind.GreaterThanEqualsToken: return (true, BinaryOperatorKind.GreaterThanOrEqual);
                case SyntaxKind.LessThanEqualsToken: return (true, BinaryOperatorKind.LessThanOrEqual);
                case SyntaxKind.AmpersandToken: return (true, BinaryOperatorKind.And);
                case SyntaxKind.BarToken: return (true, BinaryOperatorKind.Or);
                case SyntaxKind.CaretToken: return (true, BinaryOperatorKind.Xor);
                default: return (false, default);
            }
        }
    }
}
