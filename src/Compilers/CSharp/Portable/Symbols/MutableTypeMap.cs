// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Utility class for substituting actual type arguments for formal generic type parameters.
    /// </summary>
    internal sealed class MutableTypeMap : AbstractTypeParameterMap
    {
        internal MutableTypeMap()
            : base(new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>())
        {
        }

        internal void Add(TypeParameterSymbol key, TypeWithModifiers value)
        {
            this.Mapping.Add(key, value);
        }

        /// <summary>
        /// Converts this type map to an immutable unification.
        /// </summary>
        /// <returns>
        /// The unification corresponding to this type map.
        /// </returns>
        internal ImmutableTypeMap ToUnification() => new ImmutableTypeMap(Mapping);
    }

    /// <summary>
    /// Immutable type map representing a unification.
    /// </summary>
    internal sealed class ImmutableTypeMap : AbstractTypeParameterMap
    {
        /// <summary>
        /// Constructs an empty unification.
        /// </summary>
        internal ImmutableTypeMap()
            : base(new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>())
        {
        }

        /// <summary>
        /// Constructs a unification with the given dictionary.
        /// </summary>
        /// <param name="dict">
        /// The dictionary to use in construction.
        /// </param>
        internal ImmutableTypeMap(SmallDictionary<TypeParameterSymbol, TypeWithModifiers> dict)
            : base(new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>(dict, EqualityComparer<TypeParameterSymbol>.Default))
        {
        }

        /// <summary>
        /// Sequentially composes two unifications, creating a new unification.
        /// </summary>
        /// <param name="other">
        /// The RHS of the sequential composition.</param>
        /// <returns>
        /// The composed unification (this; <paramref name="other"/>). 
        /// </returns>
        internal ImmutableTypeMap Compose(ImmutableTypeMap other) => ComposeDict(other.Mapping);

        /// <summary>
        /// Sequentially composes a unification with a dictionary, creating a new unification.
        /// </summary>
        /// <param name="dict">
        /// The dictionary representing another unification to merge.</param>
        /// <returns>
        /// The composed unification (this; <paramref name="dict"/>). 
        /// </returns>

        internal ImmutableTypeMap ComposeDict(SmallDictionary<TypeParameterSymbol, TypeWithModifiers> dict)
        {
            // TODO: efficiency
            var dictM = new ImmutableTypeMap(dict);

            var result = new ImmutableTypeMap();

            // We want the resulting mapping to be dict(this(x)).
            // Thus:
            // 1) For all mappings in this (this(x) != x), take our mapping, substitute with dict, and store the resulting mapping;
            // 2) For all mappings in dict, add them back in if they are not already in the new mapping.
            foreach (var ourKey in Mapping.Keys)
            {
                result.Mapping.Add(ourKey, Mapping[ourKey].CustomModifiers.IsEmpty ? dictM.SubstituteType(Mapping[ourKey].AsTypeSymbolOnly()) : Mapping[ourKey]);
            }
            foreach (var theirKey in dict.Keys)
            {
                // This means the other mapping contained this, 
                if (result.Mapping.ContainsKey(theirKey)) continue;
                result.Mapping.Add(theirKey, dict[theirKey]);
            }

            return result;
        }

        /// <summary>
        /// Adds a mapping to this unification, creating a new unification.
        /// </summary>
        /// <param name="key">
        /// The key of the mapping to add.
        /// </param>
        /// <param name="value">
        /// The value of the mapping to add.
        /// </param>
        /// <returns>
        /// The result of adding, which will ignore this mapping if there is
        /// already a present mapping for <paramref name="key"/>. 
        /// </returns>

        internal ImmutableTypeMap Add(TypeParameterSymbol key, TypeWithModifiers value)
        {
            var sd = new SmallDictionary<TypeParameterSymbol, TypeWithModifiers>();
            sd.Add(key, value);
            return ComposeDict(sd);
        }
    }
}
