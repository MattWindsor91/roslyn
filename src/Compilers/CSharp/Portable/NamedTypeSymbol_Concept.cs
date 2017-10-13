// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal partial class NamedTypeSymbol
    {
        /// <summary>
        /// Gets whether this symbol represents a concept.
        /// </summary>
        internal abstract bool IsConcept { get; } //@t-mawind

        /// <summary>
        /// Gets whether this symbol represents a concept instance.
        /// </summary>
        internal abstract bool IsInstance { get; } //@t-mawind

        /// <summary>
        /// Gets whether this symbol represents a standalone concept instance.
        /// </summary>
        internal virtual bool IsStandaloneInstance => IsInstance && Interfaces.IsDefaultOrEmpty;
    }
}
