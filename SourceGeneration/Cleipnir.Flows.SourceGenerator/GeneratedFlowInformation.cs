namespace Cleipnir.Flows.SourceGenerator
{
    public class GeneratedFlowInformation(string generatedCode, string fileName)
    {
        public string GeneratedCode { get; } = generatedCode;
        public string FileName { get; } = fileName;

        public override int GetHashCode() => GeneratedCode.GetHashCode();

        public override bool Equals(object? obj) 
            => obj is GeneratedFlowInformation other && GeneratedCode == other.GeneratedCode;
    }
}