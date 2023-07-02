using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Cleipnir.Flows.SourceGenerator
{
    [Generator]
    public class FlowSourceGenerator : ISourceGenerator
    {
        private const string UnitFlowType = "Cleipnir.Flows.Flow`2";
        private const string ResultFlowType = "Cleipnir.Flows.Flow`3";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Initialization logic
            context.RegisterForSyntaxNotifications(() => new TypeSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var syntaxReceiver = (TypeSyntaxReceiver) context.SyntaxReceiver;
            if (syntaxReceiver == null)
            {
                AddSourceGenerationOutput(context, "no syntax receiver found");
                return;
            }

            var unitFlowType = context.Compilation.GetTypeByMetadataName(UnitFlowType);
            if (unitFlowType == null)
            {
                AddSourceGenerationOutput(context, "unable to locate abstract flow");
                return;
            }
           
            var resultFlowType = context.Compilation.GetTypeByMetadataName(ResultFlowType);
            if (resultFlowType == null)
            {
                AddSourceGenerationOutput(context, "unable to locate abstract flow");
                return;
            }

            var implementationTypes = new List<FlowInformation>();
            var foundFlows = new List<string>();
            foreach (var classDeclaration in syntaxReceiver.ClassDeclarations)
            {
                var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var flowType = (INamedTypeSymbol) semanticModel.GetDeclaredSymbol(classDeclaration);

                if (!InheritsFromFlowType(flowType, unitFlowType) && !InheritsFromFlowType(flowType, resultFlowType))
                    continue;

                var baseType = flowType.BaseType;
                var baseTypeTypeArguments = baseType.TypeArguments;

                var paramType = baseTypeTypeArguments[0];
                var scrapbookType = baseTypeTypeArguments[1];
                var resultType = baseTypeTypeArguments.Length == 3 ? baseTypeTypeArguments[2] : null;
                
                var runMethod = flowType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Single(m => m.Name == "Run" && m.IsOverride);
                var parameterName = runMethod.Parameters.Single().Name;

                foundFlows.Add(GetFullyQualifiedName(flowType));
                implementationTypes.Add(new FlowInformation(flowType, paramType, parameterName, scrapbookType, resultType));
            }

            AddSourceGenerationOutput(context, $"Found flows: {string.Join(", ", foundFlows)}");
            GenerateCode(context, implementationTypes);
        }

        private static bool InheritsFromFlowType(INamedTypeSymbol childTypeSymbol, INamedTypeSymbol parentTypeSymbol)
        {
            var baseType = childTypeSymbol.BaseType;
            if (baseType == null || !baseType.IsGenericType) return false;

            return SymbolEqualityComparer.Default.Equals(
                baseType.ConstructUnboundGenericType(),
                parentTypeSymbol.ConstructUnboundGenericType()
            );
        }

        private static string GetFullyQualifiedName(ITypeSymbol typeSymbol)
        {
            var symbolDisplayFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
            );

            var fullyQualifiedName = typeSymbol.ToDisplayString(symbolDisplayFormat);
            return fullyQualifiedName;
        }

        private static string GetNamespace(ITypeSymbol typeSymbol)
        {
            var split = GetFullyQualifiedName(typeSymbol).Split('.');
            return string.Join(".", split, startIndex: 0, split.Length - 1);
        }

        private void GenerateCode(GeneratorExecutionContext context, List<FlowInformation> flowInformations)
        {
            //AddFlows(context, flowInformations);

            foreach (var implementationType in flowInformations)
                AddFlowsWrapper2(context, implementationType);
        }

        private void AddFlows(GeneratorExecutionContext context, List<FlowInformation> flowInformations)
        {
            var flowsProperties = flowInformations
                .Select(implementationType =>
                {
                    var fqName = GetFullyQualifiedName(implementationType.FlowTypeSymbol);
                    return Spaces(8) + "public " + fqName + "s " + implementationType.FlowTypeSymbol.Name + "s { get; } = new();";
                }).Aggregate(seed: new StringBuilder(), (builder, str) => builder.AppendLine(str));
            /*
            var flowsInitialization = flowInformations
                .Select(implementationType =>
                {
                    var fqName = GetFullyQualifiedName(implementationType.FlowTypeSymbol);
                    return Spaces(12) + implementationType.FlowTypeSymbol.Name + "s = new();";
                }).Aggregate(seed: new StringBuilder(), (builder, str) => builder.AppendLine(str));
            */
            string generatedCode = 
@"namespace Cleipnir.Flows
{
    public partial class Flows
    {
" + flowsProperties + @"   
    }
}";
            context.AddSource("Flows.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }

        private void AddFlowsWrapper2(GeneratorExecutionContext context, FlowInformation flowInformation)
        {
            var flowsName = $"{flowInformation.FlowTypeSymbol.Name}s";
            var flowsNamespace = GetNamespace(flowInformation.FlowTypeSymbol);
            var flowType = GetFullyQualifiedName(flowInformation.FlowTypeSymbol);
            var flowName = flowInformation.FlowTypeSymbol.Name;
            var paramType = GetFullyQualifiedName(flowInformation.ParamTypeSymbol);
            var paramName = CamelCase(flowInformation.ParameterName);
            var scrapbookName = CamelCase(flowInformation.ScrapbookTypeSymbol.Name);
            var scrapbookType = GetFullyQualifiedName(flowInformation.ScrapbookTypeSymbol);
            var resultType = flowInformation.ResultTypeSymbol != null 
                ? GetFullyQualifiedName(flowInformation.ResultTypeSymbol)
                : null;

            var generatedCode = resultType == null
                ?
@"namespace " + flowsNamespace + @"
{
    [Cleipnir.Flows.SourceGeneration.SourceGeneratedFlowsAttribute]
    public class " + flowsName + " : Cleipnir.Flows.Flows<" + flowType + ", " + paramType + ", " + scrapbookType + @">
    {
        public " + flowsName + @"(Cleipnir.Flows.FlowsContainer flowsContainer)
            : base(" + $@"""{flowName}""" + @", flowsContainer) { }
    }
}" :
@"namespace " + flowsNamespace + @"
{
    [Cleipnir.Flows.SourceGeneration.SourceGeneratedFlowsAttribute]
    public class " + flowsName + " : Cleipnir.Flows.Flows<" + flowType + ", " + paramType + ", " + scrapbookType + ", " + resultType + @">
    {
        public " + flowsName + @"(Cleipnir.Flows.FlowsContainer flowsContainer)
            : base(" + $@"""{flowName}"""+ @", flowsContainer) { }
    }
}";

            // Add the generated code to the compilation
            context.AddSource(flowsName + ".g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }

        private string CamelCase(string str)
        {
            return char.ToLower(str[0]) + str.Substring(1);
        }

        private string Spaces(int count)
        {
            return new string(' ', count);
        }

        private class TypeSyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax> ClassDeclarations { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.BaseList != null)
                    ClassDeclarations.Add(classDeclarationSyntax);
            }
        }

        private void AddSourceGenerationOutput(GeneratorExecutionContext context, string output)
        {
            string source = 
$@"using System;

namespace Cleipnir.Flows
{{
    public static class SourceGenerationOutput
    {{
        public const string Output = ""{output}"";
    }}
}}
";
            context.AddSource($"SourceGenerationOutput.g.cs", source);
        }
    }
}