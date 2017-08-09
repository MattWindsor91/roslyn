namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Interface for interface forwarding methods.
    /// </summary>
    internal abstract class SynthesizedImplementationForwardingMethod : SynthesizedImplementationMethod
    {
        private readonly MethodSymbol _implementingMethod;

        public SynthesizedImplementationForwardingMethod(MethodSymbol interfaceMethod, MethodSymbol implementingMethod, NamedTypeSymbol implementingType)
            : base(interfaceMethod, implementingType, generateDebugInfo: false)
        {
            _implementingMethod = implementingMethod;
        }

        internal override bool SynthesizesLoweredBoundBody => true;

        public MethodSymbol ImplementingMethod => _implementingMethod;
    }
}