using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Cleipnir.Flows.SourceGenerator.Utils;

namespace Cleipnir.Flows.SourceGenerator
{
    [Generator]
    public class FlowsSourceGenerator : IIncrementalGenerator
    {
        private const string ParamlessFlowType = "Cleipnir.Flows.Flow";
        private const string UnitFlowType = "Cleipnir.Flows.Flow`1";
        private const string ResultFlowType = "Cleipnir.Flows.Flow`2";
        private const string IExposeStateType = "Cleipnir.Flows.IExposeState`1";
        private const string IgnoreAttribute = "Cleipnir.Flows.SourceGeneration.Ignore";
        
        private INamedTypeSymbol? _paramlessFlowTypeSymbol;
        private INamedTypeSymbol? _unitFlowTypeSymbol;
        private INamedTypeSymbol? _resultFlowTypeSymbol;
        private INamedTypeSymbol? _exposeStateTypeSymbol;
        private INamedTypeSymbol? _ignoreAttribute;
        
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Initialization logic
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Cleipnir.Flows.GenerateFlowsAttribute",
                (node, ctx) => node is ClassDeclarationSyntax,
                transform: (ctx, _) => Transform(ctx)
            ).Where(flowInformation => flowInformation is not null);
            
            context.RegisterSourceOutput(
                provider,
                (ctx, generatedFlowInformation) =>
                    ctx.AddSource(generatedFlowInformation!.FileName, generatedFlowInformation.GeneratedCode)
            );
        }

        public GeneratedFlowInformation? Transform(GeneratorAttributeSyntaxContext context)
        {
            
            //if (_paramlessFlowTypeSymbol == null)
            {
                var complication = context.SemanticModel.Compilation;
                _paramlessFlowTypeSymbol = complication.GetTypeByMetadataName(ParamlessFlowType);
                _unitFlowTypeSymbol = complication.GetTypeByMetadataName(UnitFlowType);
                _resultFlowTypeSymbol = complication.GetTypeByMetadataName(ResultFlowType);
                _exposeStateTypeSymbol = complication.GetTypeByMetadataName(IExposeStateType);
                if (_exposeStateTypeSymbol != null)
                    _exposeStateTypeSymbol = _exposeStateTypeSymbol.ConstructUnboundGenericType();
                _ignoreAttribute = complication.GetTypeByMetadataName(IgnoreAttribute);
            }

            if (_paramlessFlowTypeSymbol == null || _unitFlowTypeSymbol == null || _resultFlowTypeSymbol == null || _exposeStateTypeSymbol == null)
                return null;

            var classDeclaration = (ClassDeclarationSyntax) context.TargetNode;
            if (classDeclaration.Modifiers.Any(m => m.Value is "private"))
                return null;

            var accessibilityModifier = classDeclaration.Modifiers.Any(m => m.Value is "public")
                ? "public"
                : "internal";

            var semanticModel = context.SemanticModel;
            var flowType = (INamedTypeSymbol?)semanticModel.GetDeclaredSymbol(classDeclaration);
            
            if (
                flowType == null ||
                !InheritsFromParamlessFlowType(flowType, _paramlessFlowTypeSymbol) &&
                !InheritsFromFlowType(flowType, _unitFlowTypeSymbol) &&
                !InheritsFromFlowType(flowType, _resultFlowTypeSymbol)
            ) return null;

            if (flowType.ContainingType != null || flowType.IsFileLocal)
                return null;
            
            ITypeSymbol? stateTypeSymbol = null;
            foreach (var implementedInterface in flowType.AllInterfaces)
                if (
                    implementedInterface.IsGenericType &&
                    SymbolEqualityComparer.Default.Equals(implementedInterface.ConstructUnboundGenericType(), _exposeStateTypeSymbol)
                )
                {
                    stateTypeSymbol = implementedInterface.TypeArguments[0];
                    break;
                }

            var hasIgnoreAttribute = flowType
                .GetAttributes()
                .Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _ignoreAttribute));
            if (hasIgnoreAttribute)
                return null;
            
            var baseType = flowType.BaseType;
            if (baseType == null)
                return null;
            
            if (InheritsFromParamlessFlowType(flowType, _paramlessFlowTypeSymbol))
                return GenerateCode(
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

            var baseTypeTypeArguments = baseType.TypeArguments;
            var paramType = baseTypeTypeArguments.Length > 0 ? baseTypeTypeArguments[0] : null;
            var resultType = baseTypeTypeArguments.Length == 2 ? baseTypeTypeArguments[1] : null;

            var runMethod = flowType.GetMembers()
                .OfType<IMethodSymbol>()
                .Single(m => m.Name == "Run" && m.IsOverride);
            var parameterName = runMethod.Parameters.Single().Name;

            return
                GenerateCode(
                    new FlowInformation(
                        flowType,
                        paramType,
                        parameterName,
                        resultType,
                        stateTypeSymbol,
                        paramless: false,
                        accessibilityModifier
                    )
                );
        }

        private GeneratedFlowInformation GenerateCode(FlowInformation flowInformation)
        {
            var flowsName = $"{flowInformation.FlowTypeSymbol.Name}s";
            var flowsNamespace = GetNamespace(flowInformation.FlowTypeSymbol);
            var flowType = GetFullyQualifiedName(flowInformation.FlowTypeSymbol);
            var flowName = flowInformation.FlowTypeSymbol.Name;
            var paramType = flowInformation.ParamTypeSymbol == null 
                ? null 
                : GetFullyQualifiedName(flowInformation.ParamTypeSymbol);
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

        public Task<{stateType}?> GetState(string instanceId) 
            => GetState<{stateType}>(instanceId);";
                var constructorEndPosition = generatedCode.IndexOf("{ }", StringComparison.Ordinal);
                generatedCode = generatedCode.Insert(constructorEndPosition + 3, getStateStr);
            }
            
            return new GeneratedFlowInformation(generatedCode, GetFileName(flowInformation));
        }
    }
}