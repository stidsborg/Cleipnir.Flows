using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Presentation.Examples;

public class ScrapbookFlow : Flow<string, ScrapbookFlow.FlowScrapbook>
{
    public override async Task Run(string param)
    {
        if (Scrapbook.Started == null)
        {
            Scrapbook.Started = DateTime.Now;
            await Scrapbook.Save();
        }

        Console.WriteLine("Flow was initially started: " + Scrapbook.Started);
    }

    public class FlowScrapbook : RScrapbook
    {
        public DateTime? Started { get; set; }
    }
}