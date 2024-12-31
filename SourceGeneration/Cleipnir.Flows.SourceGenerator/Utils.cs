using System.Linq;
using Microsoft.CodeAnalysis;

namespace Cleipnir.Flows.SourceGenerator;

internal static class Utils
{
    public static string GetFileName(FlowInformation flowInformation)
    {
        var fullyQualifiedName = GetFullyQualifiedName(flowInformation.FlowTypeSymbol);
        return fullyQualifiedName + ".g.cs";
    }
    
    public static string GetNamespace(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ContainingNamespace.ToString();
    }
    
    public static string GetFullyQualifiedName(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
            return GetFullyQualifiedName((ITypeSymbol) typeSymbol);

        var baseType = GetFullyQualifiedName((ITypeSymbol) typeSymbol);
        var genericArguments = typeSymbol.TypeArguments.Select(GetFullyQualifiedName);
        var comma = ",";
        return $"{baseType}<{string.Join(comma, genericArguments)}>";
    }
    
    public static string GetFullyQualifiedName(ITypeSymbol typeSymbol)
    {
        var symbolDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
        );

        var fullyQualifiedName = typeSymbol.ToDisplayString(symbolDisplayFormat);
        return fullyQualifiedName;
    }
    
    public static bool InheritsFromParamlessFlowType(INamedTypeSymbol childTypeSymbol, INamedTypeSymbol parentTypeSymbol) 
        => SymbolEqualityComparer.Default.Equals(childTypeSymbol.BaseType, parentTypeSymbol);
        
    public static bool InheritsFromFlowType(INamedTypeSymbol childTypeSymbol, INamedTypeSymbol parentTypeSymbol)
    {
        var baseType = childTypeSymbol.BaseType;
        if (baseType == null || !baseType.IsGenericType) return false;

        return SymbolEqualityComparer.Default.Equals(
            baseType.ConstructUnboundGenericType(),
            parentTypeSymbol.ConstructUnboundGenericType()
        );
    }
}