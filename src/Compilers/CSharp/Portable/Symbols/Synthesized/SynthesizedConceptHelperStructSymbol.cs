// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A nested, synthesised struct that enables some advanced concept
    /// feature, eg. defaults or inline instances.
    /// </summary>
    internal abstract class SynthesizedConceptHelperStructSymbol : SynthesizedContainer
    {
        /// <summary>
        /// The parent symbol.
        /// </summary>
        private readonly SourceNamedTypeSymbol _parent;

        public sealed override Symbol ContainingSymbol => _parent;

        //   A helper struct is, of course, a struct...
        public sealed override TypeKind TypeKind => TypeKind.Struct;

        //   ...and, as it has no fields, its layout is specified as the
        //   minimum allowed by the CLI spec (1).
        //   This override is necessary, as otherwise the generated PE is
        //   invalid.
        internal sealed override TypeLayout Layout =>
            new TypeLayout(LayoutKind.Sequential, 1, alignment: 0);


        //   Helper structs have to be public, else they're useless.
        public sealed override Accessibility DeclaredAccessibility => Accessibility.Public;

        private ImmutableArray<Symbol> _members;

        protected SynthesizedConceptHelperStructSymbol(string name, SourceNamedTypeSymbol parent, Func<SynthesizedContainer, ImmutableArray<TypeParameterSymbol>> typeParametersF, TypeMap typeMap) : base(name, typeParametersF, typeMap)
        {
            _parent = parent;
        }

        public sealed override ImmutableArray<Symbol> GetMembers()
        {
            //   Not making this lazy results in new symbols being created every
            //   time we call GetMembers(), which is not only inefficient but
            //   breaks reference equality.
            if (_members.IsDefault)
            {
                var mb = ArrayBuilder<Symbol>.GetInstance();
                mb.AddRange(base.GetMembers());
                //   This is slightly wrong, but we don't have any syntax to
                //   cling onto apart from this...
                var binder = DeclaringCompilation.GetBinder(_parent.GetNonNullSyntaxNode());
                var diagnostics = DiagnosticBag.GetInstance();

                MakeMembers(mb, binder, diagnostics);
                AddDeclarationDiagnostics(diagnostics);

                ImmutableInterlocked.InterlockedInitialize(ref _members, mb.ToImmutableAndFree());
            }
            return _members;
        }

        protected abstract void MakeMembers(ArrayBuilder<Symbol> mb, Binder binder, DiagnosticBag diagnostics);

        public sealed override ImmutableArray<Symbol> GetMembers(string name)
        {
            // TODO(@MattWindsor91): slow and ugly.

            var mb = ArrayBuilder<Symbol>.GetInstance();
            mb.AddRange(base.GetMembers(name));

            foreach (var m in GetMembers())
            {
                if (m.Name == name) mb.Add(m);
            }

            return mb.ToImmutableAndFree();
        }
    }
}
