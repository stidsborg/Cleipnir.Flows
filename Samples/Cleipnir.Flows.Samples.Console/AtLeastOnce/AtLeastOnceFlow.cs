namespace Cleipnir.Flows.Sample.ConsoleApp.AtLeastOnce;

[GenerateFlows]
public class AtLeastOnceFlow : Flow<string, string>
{
    private readonly PuzzleSolverService _puzzleSolverService = new();

    public override async Task<string> Run(string hashCode)
    {
        var solution = await Capture(
            "SolvePuzzle",
            work: () => _puzzleSolverService.SolveCryptographicPuzzle(hashCode)
        );
        
        return solution;
    }
}