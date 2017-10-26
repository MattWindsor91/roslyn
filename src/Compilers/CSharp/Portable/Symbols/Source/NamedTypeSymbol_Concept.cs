// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // @MattWindsor91 (Concept-C# 2016 and 2017)
    // Added getters etc. for looking up concept miscellanea on named types.

    internal partial class NamedTypeSymbol
    {
        /// <summary>
        /// Gets whether this symbol has the concept attribute set.
        /// </summary>
        /// <returns>
        /// True if this symbol has the <c>System_Concepts_ConceptAttribute</c> attribute;
        /// false otherwise.
        ///</returns>
        internal bool HasConceptAttribute
        {
            get
            {
                foreach (var attribute in GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets whether this symbol has the instance attribute set.
        /// </summary>
        /// <returns>
        /// True if this symbol has the <c>System_Concepts_ConceptInstanceAttribute</c>
        /// attribute; false otherwise.
        ///</returns>
        internal bool HasInstanceAttribute
        {
            get
            {
                foreach (var attribute in this.GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptInstanceAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        /// <summary>
        /// Gets whether this type is a default struct.
        /// </summary>
        internal virtual bool IsDefaultStruct
        {
            get
            {
                foreach (var attribute in GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptDefaultAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        /// <summary>
        /// Gets whether this type is an inline instance struct.
        /// </summary>
        internal virtual bool IsInlineInstanceStruct
        {
            get
            {
                foreach (var attribute in GetAttributes())
                {
                    if (attribute.IsTargetAttribute(this, AttributeDescription.ConceptInlineInstanceAttribute))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Attempts to find this concept's associated default struct.
        /// </summary>
        /// <returns>
        /// Null, if the default struct was not found; the struct, otherwise.
        /// </returns>
        internal NamedTypeSymbol GetDefaultStruct()
        {
            Debug.Assert(IsConcept, "Should never get the default struct of a non-concept");

            foreach (var m in GetTypeMembers())
            {
                if (m.IsDefaultStruct)
                {
                    return m;
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to find this concept's associated inline instance struct.
        /// </summary>
        /// <returns>
        /// Null, if the inline instance struct was not found; the struct, otherwise.
        /// </returns>
        internal NamedTypeSymbol GetInlineInstanceStruct()
        {
            foreach (var m in GetTypeMembers())
            {
                if (m.IsInlineInstanceStruct)
                {
                    return m;
                }
            }

            return null;
        }

        private enum OverlapType
        {
            Initialized = 0x1,
            Overlapping = 0x2,
            Overlappable = 0x4
        }
        private int _overlap = 0;
        private OverlapType GetOverlap()
        {
            if ((_overlap & (int)OverlapType.Initialized) == 0)
            {
                OverlapType o = OverlapType.Initialized;

                // Only instances can be overlapping or overlappable.
                if (IsInstance)
                {
                    foreach (var attribute in GetAttributes())
                    {
                        if (attribute.IsTargetAttribute(this, AttributeDescription.OverlappableAttribute))
                        {
                            o |= OverlapType.Overlappable;
                        }
                        else if (attribute.IsTargetAttribute(this, AttributeDescription.OverlappingAttribute))
                        {
                            o |= OverlapType.Overlapping;
                        }
                    }
                }
                Interlocked.CompareExchange(ref _overlap, (int)o, 0);
            }
            return (OverlapType)_overlap;
        }

        /// <summary>
        /// True if this type is an overlapping instance; false otherwise.
        /// </summary>
        internal bool IsOverlapping => ((GetOverlap() & OverlapType.Overlapping) != 0);

        /// <summary>
        /// True if this type is an overlappable instance; false otherwise.
        /// </summary>
        internal bool IsOverlappable => ((GetOverlap() & OverlapType.Overlappable) != 0);

        private ImmutableArray<TypeParameterSymbol> _conceptWitnesses;
        private ImmutableArray<TypeParameterSymbol> _associatedTypes;
        private int _implicitTypeParameterCount = -1;

        /// <summary>
        /// Returns the type parameters of this type that are concept
        /// witnesses.
        /// </summary>
        internal ImmutableArray<TypeParameterSymbol> ConceptWitnesses
        {
            get
            {
                if (_conceptWitnesses.IsDefault)
                {
                    var builder = new ArrayBuilder<TypeParameterSymbol>();
                    var allParams = TypeParameters;
                    int numParams = allParams.Length;
                    for (int i = 0; i < numParams; i++)
                    {
                        if (allParams[i].IsConceptWitness) builder.Add(allParams[i]);
                    }
                    ImmutableInterlocked.InterlockedInitialize(ref _conceptWitnesses, builder.ToImmutableAndFree());
                }
                return _conceptWitnesses;
            }
        }


        /// <summary>
        /// Returns the type parameters of this type that are associated
        /// types.
        /// </summary>
        internal ImmutableArray<TypeParameterSymbol> AssociatedTypes
        {
            get
            {
                if (_associatedTypes.IsDefault)
                {
                    var builder = new ArrayBuilder<TypeParameterSymbol>();
                    var allParams = TypeParameters;
                    int numParams = allParams.Length;
                    for (int i = 0; i < numParams; i++)
                    {
                        if (allParams[i].IsAssociatedType) builder.Add(allParams[i]);
                    }
                    ImmutableInterlocked.InterlockedInitialize(ref _associatedTypes, builder.ToImmutableAndFree());
                }
                return _associatedTypes;
            }
        }

        /// <summary>
        /// Returns the number of implicit type parameters.
        /// <para>
        /// These are the concept witnesses and associated types.
        /// </para>
        /// <para>
        /// This count is used for part-inference, mainly.
        /// </para>
        /// </summary>
        internal virtual int ImplicitTypeParameterCount
        {
            get
            {
                if (-1 == _implicitTypeParameterCount)
                {
                    var count = ConceptWitnesses.Length + AssociatedTypes.Length;
                    Interlocked.CompareExchange(ref _implicitTypeParameterCount, count, -1);
                }
                return _implicitTypeParameterCount;
            }
        }


        /// <summary>
        /// Tries to synthesize a shim to fill in a given concept method for
        /// this instance.
        /// </summary>
        /// <param name="concept">
        /// The concept containing the method to be shimmed.
        /// </param>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <param name="diagnostics">
        /// A diagnostics set, to which we report default struct failures.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim;
        /// otherwise, the created shim.
        /// </returns>
        protected SynthesizedInstanceShimMethod TrySynthesizeInstanceShim(NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            Debug.Assert(concept.IsConcept, "concept for default implementation synthesis must be an actual concept");
            Debug.Assert(IsInstance, "target for default implementation synthesis must be an instance");

            var ometh = TrySynthesizeOperatorShim(conceptMethod);
            if (ometh != null && ometh.IsValid())
            {
                return ometh;
            }
            var emeth = TrySynthesizeConceptExtensionShim(conceptMethod);
            if (emeth != null && emeth.IsValid())
            {
                return emeth;
            }

            // Intentionally synthesize defaults as a last resort.
            var dmeth = TrySynthesizeDefaultShim(concept, conceptMethod, diagnostics);
            if (dmeth != null && dmeth.IsValid())
            {
                return dmeth;
            }

            return null;
        }

        #region Shim synthesis
        /// <summary>
        /// Tries to generate an instance shim mapping a concept operator
        /// to a static operator on the operator's first parameter type.
        /// </summary>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// The shim might not be valid: this must be checked before acceptance.
        /// </returns>
        private SynthesizedOperatorShimMethod TrySynthesizeOperatorShim(MethodSymbol conceptMethod)
        {
            // TODO(MattWindsor91): consider opening this up, with
            //     restrictions, to any static method.
            if (!conceptMethod.IsOperator())
            {
                return null;
            }
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "concept operator should have at least one parameter");
            return new SynthesizedOperatorShimMethod(conceptMethod, this);
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept extension method
        /// to an actual method on its 'this' parameter's type.
        /// </summary>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// The shim might not be valid: this must be checked before acceptance.
        /// </returns>
        private SynthesizedConceptExtensionShimMethod TrySynthesizeConceptExtensionShim(MethodSymbol conceptMethod)
        {
            if (!conceptMethod.IsConceptExtensionMethod)
            {
                return null;
            }
            Debug.Assert(0 < conceptMethod.ParameterCount,
                "concept extension method should have a 'this' parameter");
            return new SynthesizedConceptExtensionShimMethod(conceptMethod, this);
        }

        /// <summary>
        /// Tries to generate an instance shim mapping a concept method to
        /// one on the given default struct.
        /// </summary>
        /// <param name="concept">
        /// The concept containing the default struct.
        /// </param>
        /// <param name="conceptMethod">
        /// The concept method we're implementing on an instance with a shim.
        /// </param>
        /// <param name="diagnostics">
        /// A diagnostics set, to which we report default struct failures.
        /// </param>
        /// <returns>
        /// Null if we couldn't synthesize a shim; otherwise, the created shim.
        /// The shim might not be valid: this must be checked before acceptance.
        /// </returns>
        private SynthesizedDefaultShimMethod TrySynthesizeDefaultShim(NamedTypeSymbol concept, MethodSymbol conceptMethod, DiagnosticBag diagnostics)
        {
            var dstr = concept.GetDefaultStruct();
            if (dstr == null)
            {
                return null;
            }

            // Default-struct sanity checking
            var conceptLoc = concept.Locations.IsEmpty ? Location.None : Locations[0];
            var instanceLoc = Locations.IsEmpty ? Location.None : Locations[0];
            if (dstr.Arity != 1)
            {
                // Don't use the default struct's location: it is an
                // implementation detail and may not actually exist.
                diagnostics.Add(ErrorCode.ERR_DefaultStructBadArity, conceptLoc, concept.Name, dstr.Arity, concept.Arity + 1);
                return null;
            }
            var witnessPar = dstr.TypeParameters[0];
            if (!witnessPar.IsConceptWitness)
            {
                diagnostics.Add(ErrorCode.ERR_DefaultStructNoWitnessParam, conceptLoc, concept.Name);
                return null;
            }

            return new SynthesizedDefaultShimMethod(conceptMethod, dstr, this);
        }
        #endregion Shim synthesis
    }
}
