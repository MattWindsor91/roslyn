// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        // @MattWindsor91 (Concept-C# 2017):
        //
        // Functions for supporting rewriting of concept witness invocations.
        //
        // TODO: This and everything calling into it probably wants to go into
        // a new rewriter.

        /// <summary>
        /// All concept witnesses that have been seen during this local
        /// rewriting stage and require local dictionaries constructing.
        /// Each witness is mapped to its local dictionary.
        /// The local dictionaries must all be inserted into the method's
        /// local list later on.
        /// </summary>
        private SmallDictionary<TypeSymbol, LocalSymbol> _conceptWitnessesToHoist;

        /// <summary>
        /// Synthesizes the correct receiver of a witness invocation.
        /// </summary>
        /// <param name="syntax">The syntax to be attached to the node.</param>
        /// <param name="witness">The witness we are calling into.</param>
        /// <returns>
        /// The appropriate receiver for this witness.
        /// If we're in a block, this will be a local variable;
        /// otherwise, this will be a <c>default()</c>.
        /// </returns>
        private BoundExpression SynthesizeWitnessReceiver(SyntaxNode syntax, TypeSymbol witness)
        {
            Debug.Assert(syntax != null, "Syntax for witness receiver should not be null");
            Debug.Assert(witness != null, "Witness receiver should not be null");
            Debug.Assert(witness.IsInstanceType() || witness.IsConceptWitness, "Witness receiver should be a valid witness");

            // If we're not in a block, we can't synthesise a local
            if (_rootStatement.Kind != BoundKind.Block)
            {
                return new BoundDefaultExpression(syntax, witness) { WasCompilerGenerated = true };
            }

            // TODO(@MattWindsor91): this is probably inefficient
            if (_conceptWitnessesToHoist == null)
            {
                _conceptWitnessesToHoist = new SmallDictionary<TypeSymbol, LocalSymbol>();
            }
            if (!_conceptWitnessesToHoist.ContainsKey(witness))
            {
                _conceptWitnessesToHoist.Add(witness, WitnessDictionaryLocal(witness, syntax));
            }

            var local = _conceptWitnessesToHoist[witness];
            return new BoundLocal(syntax, local, null, witness) { WasCompilerGenerated = true };
        }

        /// <summary>
        /// Constructs a local variable symbol for a concept witness
        /// dictionary.
        /// </summary>
        /// <param name="witness">
        /// The type of the witness for which we are making a dictionary.
        /// </param>
        /// <param name="syntax">
        /// The node corresponding to where the dictionary is being first
        /// invoked.
        /// </param>
        /// <returns>
        /// A local variable symbol representing the dictionary, whicm ust be
        /// inserted into the locals of the owning method.
        /// </returns>
        private LocalSymbol WitnessDictionaryLocal(TypeSymbol witness, SyntaxNode syntax)
        {
            return new SynthesizedLocal(null, witness, SynthesizedLocalKind.ConceptDictionary, syntax);
        }

        /// <summary>
        /// Decides whether a bound call is calling a concept witness.
        ///
        /// <para>
        /// These kinds of call need to be lowered into a dictionary call,
        /// because they expect an instance of their witness type but
        /// currently reference the type itself as their receiver.
        /// (This, unsurprisingly, isn't correct IL!)
        /// </para>
        /// </summary>
        /// <param name="node">
        /// The node to check.
        /// </param>
        /// <returns>
        /// True if <paramref name="node"/> is a concept witness call;
        /// false otherwise.
        /// </returns>
        private static bool IsConceptWitnessCall(BoundCall node)
        {
            Debug.Assert(node != null, "Call being checked for concept witness should not be null");
            Debug.Assert(node.Method != null, "Call being checked for concept witness should not have a null method");
            var isInstanceMethod = !node.Method.IsStatic;
            return isInstanceMethod && IsConceptWitnessReceiver(node.ReceiverOpt);
        }

        /// <summary>
        /// Decides whether a bound property access targets concept witness.
        ///
        /// <para>
        /// These kinds of access need to be lowered into a dictionary call,
        /// as they expect an instance of their witness type but
        /// currently reference the type itself as their receiver.
        /// (This, unsurprisingly, isn't correct IL!)
        /// </para>
        /// </summary>
        /// <param name="node">
        /// The node to check.
        /// </param>
        /// <returns>
        /// True if <paramref name="node"/> is a concept witness property
        /// access; false otherwise.
        /// </returns>
        private static bool IsConceptWitnessPropertyAccess(BoundPropertyAccess node)
        {
            Debug.Assert(node != null, "Property access being checked for concept witness should not be null");
            Debug.Assert(node.PropertySymbol != null, "Property access being checked for concept witness should not have a null method");

            // Concept property accesses are instance-level
            // and have a concept witness receiver.
            // (Even implicit accesses will have been rewritten to have a
            //  receiver by now.)
            var isInstanceProperty = !node.PropertySymbol.IsStatic;
            return isInstanceProperty && IsConceptWitnessReceiver(node.ReceiverOpt);
        }

        /// <summary>
        /// Decides whether a call or property receiver targets a concept
        /// witness.
        /// </summary>
        /// <param name="receiverOpt">
        /// The receiver to check; may be null.
        /// </param>
        /// <returns>
        /// True if, and only if, the receiver is a valid witness
        /// (either a ground instance, or a witness parameter).
        /// </returns>
        private static bool IsConceptWitnessReceiver(BoundExpression receiverOpt)
        {
            // Concept witness receivers are valid type expressions...
            if (receiverOpt == null || receiverOpt.Kind != BoundKind.TypeExpression)
            {
                return false;
            }

            // ...that are either ground instances or witness parameters.
            return receiverOpt.Type.IsInstanceType() || receiverOpt.Type.IsConceptWitness;
        }
    }
}
