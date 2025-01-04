using Cleipnir.Flows.Sample.MicrosoftOpen.Flows;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderMessageDrivenController(MessageDrivenOrderFlows orderFlows) : ControllerBase
{
    private readonly ILogger _logger = Log.Logger.ForContext<OrderController>();

    [HttpPost]
    public async Task<ActionResult> Post(Order order)
    {
        _logger.Information("Started processing {OrderId}", order.OrderId);
        await orderFlows.Schedule(order.OrderId, order);
        _logger.Information("Completed processing {OrderId}", order.OrderId);
        
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string orderId)
    {
        var controlPanel = await orderFlows.ControlPanel(orderId);
        if (controlPanel is null)
            return NotFound();


        return Ok(controlPanel.ToPrettyString());
    }
}
