using Microsoft.CodeAnalysis;

namespace Cleipnir.Flows.SourceGenerator
{
    internal class FlowInformation
    {
        public INamedTypeSymbol FlowTypeSymbol { get; }
        public INamedTypeSymbol? ParamTypeSymbol { get; }
        public string? ParameterName { get; }
        public INamedTypeSymbol? ResultTypeSymbol { get; }
        public bool Paramless { get; }
        public string AccessibilityModifier { get; }

        public FlowInformation(
            INamedTypeSymbol flowType, 
            INamedTypeSymbol? paramType, 
            string? parameterName, 
            INamedTypeSymbol? resultType, 
            bool paramless, 
            string accessibilityModifier)
        {
            FlowTypeSymbol = flowType;
            ParamTypeSymbol = paramType;
            ParameterName = parameterName;  
            ResultTypeSymbol = resultType;
            Paramless = paramless;
            AccessibilityModifier = accessibilityModifier;
        }
    }
}
