using Microsoft.CodeAnalysis;

namespace Cleipnir.Flows.SourceGenerator
{
    internal class FlowInformation
    {
        public INamedTypeSymbol FlowTypeSymbol { get; }
        public ITypeSymbol ParamTypeSymbol { get; }
        public string ParameterName { get; }
        public ITypeSymbol ResultTypeSymbol { get; }

        public FlowInformation(INamedTypeSymbol flowType, ITypeSymbol paramType, string parameterName, ITypeSymbol resultType)
        {
            FlowTypeSymbol = flowType;
            ParamTypeSymbol = paramType;
            ParameterName = parameterName;  
            ResultTypeSymbol = resultType;
        }
    }
}
