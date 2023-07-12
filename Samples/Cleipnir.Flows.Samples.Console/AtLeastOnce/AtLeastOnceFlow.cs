using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Console.AtLeastOnce;

public class AtLeastOnceFlow : Flow<string, AtLeastOnceFlowScrapbook, string>
{
    private readonly PuzzleSolverService _puzzleSolverService = new();

    public override async Task<string> Run(string hashCode)
    {
        var solution = await DoAtLeastOnce(
            workStatus: scrapbook => scrapbook.SolutionStatusAndResult,
            work: () => _puzzleSolverService.SolveCryptographicPuzzle(hashCode)
        );

        return solution;
    }
}

public class AtLeastOnceFlowScrapbook : RScrapbook
{
    public WorkStatusAndResult<string> SolutionStatusAndResult { get; set; }
}