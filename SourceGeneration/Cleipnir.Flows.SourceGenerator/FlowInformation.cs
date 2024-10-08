﻿using Microsoft.CodeAnalysis;

namespace Cleipnir.Flows.SourceGenerator
{
    internal class FlowInformation
    {
        public INamedTypeSymbol FlowTypeSymbol { get; }
        public ITypeSymbol? ParamTypeSymbol { get; }
        public string? ParameterName { get; }
        public ITypeSymbol? ResultTypeSymbol { get; }
        public ITypeSymbol? StateTypeSymbol { get; }
        public bool Paramless { get; }
        public string AccessibilityModifier { get; }

        public FlowInformation(
            INamedTypeSymbol flowType, 
            ITypeSymbol? paramType, 
            string? parameterName, 
            ITypeSymbol? resultType, 
            ITypeSymbol? stateTypeSymbol,
            bool paramless, 
            string accessibilityModifier)
        {
            FlowTypeSymbol = flowType;
            ParamTypeSymbol = paramType;
            ParameterName = parameterName;  
            ResultTypeSymbol = resultType;
            StateTypeSymbol = stateTypeSymbol;
            Paramless = paramless;
            AccessibilityModifier = accessibilityModifier;
        }
    }
}
