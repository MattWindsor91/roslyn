// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// @t-mawind
//   This is just an API bridge to allow the existing implicit-keyword-less
//   syntax API to work.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public sealed partial class TypeParameterSyntax
    {
        public TypeParameterSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxToken varianceKeyword, SyntaxToken identifier)
        {
            return Update(
                attributeLists,
                varianceKeyword,
                default(SyntaxToken),
                identifier);
        }
    }
}
