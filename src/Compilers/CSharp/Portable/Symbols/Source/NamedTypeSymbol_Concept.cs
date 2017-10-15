﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Gets the name of this type's associated default struct.
        /// </summary>
        internal string DefaultStructName
        {
            get
            {
                Debug.Assert(IsConcept, "Should never get the default struct name of a non-concept");
                // @t-mawind TODO: use a non-referenceable name
                return $"{Name}_default";
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
    }
}
