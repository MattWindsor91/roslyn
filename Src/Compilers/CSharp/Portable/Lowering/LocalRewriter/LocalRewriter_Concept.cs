// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        //
        // @MattWindsor91 (Concept-C# 2017):
        //
        // Functions for supporting rewriting of concept witness invocations.
        //
        // TODO: This and everything calling into it probably wants to go into
        // a new rewriter.
        //

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
        /// <param name="syntax">
        /// The syntax from which the receiver is being synthesized.
        /// </param>
        /// <param name="witness">
        /// The witness on which we are invoking a method.
        /// </param>
        /// <returns></returns>
        private BoundExpression SynthesizeWitnessReceiver(SyntaxNode syntax, TypeSymbol witness)
        {
            Debug.Assert(syntax != null, "Syntax for witness receiver should not be null");
            Debug.Assert(witness != null, "Witness receiver should not be null");
            Debug.Assert(witness.IsInstanceType() || witness.IsConceptWitness, "Witness receiver should be a valid witness");

            // @MattWindsor91
            // TODO: this is probably inefficient
            if (_conceptWitnessesToHoist == null)
            {
                _conceptWitnessesToHoist = new SmallDictionary<TypeSymbol, LocalSymbol>();
            }
            if (!_conceptWitnessesToHoist.ContainsKey(witness))
            {
                _conceptWitnessesToHoist.Add(witness, WitnessDictionaryLocal(witness, syntax));
            }

            // @MattWindsor91
            // TODO: hoist this default creation, somehow.
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

            // Concept witness calls are instance calls...
            if (node.Method.IsStatic)
            {
                return false;
            }

            // ...that have a valid, type expression receiver...
            var ro = node.ReceiverOpt;
            if (ro == null || ro.Kind != BoundKind.TypeExpression)
            {
                return false;
            }

            // ...that is either a ground instance or a witness parameter.
            return node.ReceiverOpt.Type.IsInstanceType() || node.ReceiverOpt.Type.IsConceptWitness;
        }
    }
}
