using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Batch;
using Microsoft.AspNetCore.Mvc;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class BatchOrderController(BatchOrderFlows batchOrderFlows) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Post(OrdersBatch ordersBatch)
    {
        await batchOrderFlows.Schedule(ordersBatch.BatchId, ordersBatch.Orders);
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string batchId)
    {
        var controlPanel = await batchOrderFlows.ControlPanel(batchId);
        if (controlPanel is null)
            return NotFound();
        
        return Ok(await controlPanel.ToPrettyString());
    }
}