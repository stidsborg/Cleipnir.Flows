using System;
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
        private const string ParamlessFlowType = "Cleipnir.Flows.Flow";
        private const string UnitFlowType = "Cleipnir.Flows.Flow`1";
        private const string ResultFlowType = "Cleipnir.Flows.Flow`2";
        private const string IHaveStateType = "Cleipnir.Flows.IHaveState`1";

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

            var paramlessFlowType = context.Compilation.GetTypeByMetadataName(ParamlessFlowType);
            if (paramlessFlowType == null)
            {
                AddSourceGenerationOutput(context, "unable to locate abstract flow");
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
            
            var iHaveStateType = context.Compilation.GetTypeByMetadataName(IHaveStateType);
            if (iHaveStateType == null)
            {
                AddSourceGenerationOutput(context, "unable to locate IHaveState type");
                return;
            }
            iHaveStateType = iHaveStateType.ConstructUnboundGenericType();

            var implementationTypes = new List<FlowInformation>();
            foreach (var classDeclaration in syntaxReceiver.ClassDeclarations)
            {
                if (classDeclaration.Modifiers.Any(m => m.Value is "private"))
                    continue;
                
                var accessibilityModifier = classDeclaration.Modifiers.Any(m => m.Value is "public")
                        ? "public"
                        : "internal";
                
                var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var flowType = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(classDeclaration);

                if (
                    flowType == null ||
                    !InheritsFromParamlessFlowType(flowType, paramlessFlowType) &&
                    !InheritsFromFlowType(flowType, unitFlowType) &&
                    !InheritsFromFlowType(flowType, resultFlowType)
                ) continue;

                ITypeSymbol stateTypeSymbol = null;
                foreach (var implementedInterface in flowType.AllInterfaces)
                    if (
                        implementedInterface.IsGenericType && 
                        SymbolEqualityComparer.Default.Equals(implementedInterface.ConstructUnboundGenericType(), iHaveStateType)
                    )
                    {
                        stateTypeSymbol = implementedInterface.TypeArguments[0];
                        break;
                    }
                
                if (InheritsFromParamlessFlowType(flowType, paramlessFlowType))
                {
                    implementationTypes.Add(
                        new FlowInformation(
                            flowType,
                            paramType: null,
                            parameterName: null,
                            resultType: null,
                            stateTypeSymbol,
                            paramless: true,
                            accessibilityModifier
                        )
                    );
                    continue;
                }

                var baseType = flowType.BaseType;
                if (baseType == null)
                    continue;
                
                var baseTypeTypeArguments = baseType.TypeArguments;

                var paramType = baseTypeTypeArguments.Length > 0 ? baseTypeTypeArguments[0] : null;
                var resultType = baseTypeTypeArguments.Length == 2 ? baseTypeTypeArguments[1] : null;

                var runMethod = flowType.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Single(m => m.Name == "Run" && m.IsOverride);
                var parameterName = runMethod.Parameters.Single().Name;
                
                implementationTypes.Add(
                    new FlowInformation(flowType, paramType, parameterName, resultType, stateTypeSymbol, paramless: false, accessibilityModifier)
                );
            }

            GenerateCode(context, implementationTypes);
        }
        
        private static bool InheritsFromParamlessFlowType(INamedTypeSymbol childTypeSymbol, INamedTypeSymbol parentTypeSymbol) 
            => SymbolEqualityComparer.Default.Equals(childTypeSymbol.BaseType, parentTypeSymbol);
        
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
            return typeSymbol.ContainingNamespace.ToString();
        }

        private void GenerateCode(GeneratorExecutionContext context, List<FlowInformation> flowInformations)
        {
            var flowNames = new Dictionary<string, int>();
            foreach (var implementationType in flowInformations)
                AddFlowsWrapper(context, implementationType, flowNames);
        }

        private void AddFlowsWrapper(GeneratorExecutionContext context, FlowInformation flowInformation, Dictionary<string, int> flowNames)
        {
            var flowsName = $"{flowInformation.FlowTypeSymbol.Name}s";
            var flowsNamespace = GetNamespace(flowInformation.FlowTypeSymbol);
            var flowType = GetFullyQualifiedName(flowInformation.FlowTypeSymbol);
            var flowName = flowInformation.FlowTypeSymbol?.Name;
            var paramType = flowInformation.ParamTypeSymbol == null 
                ? null 
                : GetFullyQualifiedName(flowInformation.ParamTypeSymbol);
            //var paramName = CamelCase(flowInformation.ParameterName);
            var resultType = flowInformation.ResultTypeSymbol != null 
                ? GetFullyQualifiedName(flowInformation.ResultTypeSymbol)
                : null;
            var stateType = flowInformation.StateTypeSymbol == null
                ? null
                : GetFullyQualifiedName(flowInformation.StateTypeSymbol);

            var accessibilityModifier = flowInformation.AccessibilityModifier;
            
            string generatedCode;
            if (flowInformation.Paramless)
            {
                generatedCode = 
$@"namespace {flowsNamespace}
{{
    #nullable enable
    [Cleipnir.Flows.SourceGeneration.SourceGeneratedFlowsAttribute]
    {accessibilityModifier} class {flowsName} : Cleipnir.Flows.Flows<{flowType}>
    {{        
        public {flowsName}(Cleipnir.Flows.FlowsContainer flowsContainer, string flowName = ""{flowName}"", Cleipnir.Flows.Options? options = null)
            : base(flowName, flowsContainer, options) {{ }}             
    }}
    #nullable disable   
}}";                
            }
            else if (resultType == null)
            {
                generatedCode = 
$@"namespace {flowsNamespace}
{{
    #nullable enable
    [Cleipnir.Flows.SourceGeneration.SourceGeneratedFlowsAttribute]
    {accessibilityModifier} class {flowsName} : Cleipnir.Flows.Flows<{flowType}, {paramType}>
    {{
        public {flowsName}(Cleipnir.Flows.FlowsContainer flowsContainer, string flowName = ""{flowName}"", Cleipnir.Flows.Options? options = null)
            : base(flowName, flowsContainer, options) {{ }}      
    }}
    #nullable disable
}}";
            }
            else
            {
                generatedCode = 
$@"namespace {flowsNamespace}
{{
    #nullable enable
    [Cleipnir.Flows.SourceGeneration.SourceGeneratedFlowsAttribute]
    {accessibilityModifier} class {flowsName} : Cleipnir.Flows.Flows<{flowType}, {paramType}, {resultType}>
    {{
        public {flowsName}(Cleipnir.Flows.FlowsContainer flowsContainer, string flowName = ""{flowName}"", Cleipnir.Flows.Options? options = null)
            : base(flowName, flowsContainer, options) {{ }}
    }}
    #nullable disable
}}";
            }

            if (flowInformation.StateTypeSymbol != null)
            {   
                var getStateStr = $@"

        public Task<{stateType}?> GetState(string functionInstanceId) 
            => GetState<{stateType}>(functionInstanceId);";
                var constructorEndPosition = generatedCode.IndexOf("{ }", StringComparison.Ordinal);
                generatedCode = generatedCode.Insert(constructorEndPosition + 3, getStateStr);
            }
            
            // Add the generated code to the compilation
            var fileName =
                !flowNames.ContainsKey(flowName)
                    ? flowsName + ".g.cs"
                    : flowsName + $"{flowNames[flowName]}.g.cs";
            if (flowNames.ContainsKey(flowName))
                flowNames[flowName] += 1;
            else
                flowNames[flowName] = 1;
            
            context.AddSource(fileName, SourceText.From(generatedCode, Encoding.UTF8));
        }

        private string CamelCase(string str)
        {
            return char.ToLower(str[0]) + str.Substring(1);
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
            var descriptor = new DiagnosticDescriptor(
                id: "CLEIPNIR",
                title: "Source Generator Error",
                messageFormat: "{0}",
                category: "Design",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            );

            context.ReportDiagnostic(
                Diagnostic.Create(descriptor, Location.None, output)
            );
        }
    }
}