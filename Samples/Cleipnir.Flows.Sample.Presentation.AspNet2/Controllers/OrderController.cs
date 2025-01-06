using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController(Flows.Rpc.OrderFlows orderFlows) : ControllerBase
{
    private readonly ILogger _logger = Log.Logger.ForContext<OrderController>();

    [HttpPost]
    public async Task<ActionResult> Post(Order order)
    {
        _logger.Information("Started processing {OrderId}", order.OrderId);
        await orderFlows.Run(order.OrderId, order);
        _logger.Information("Completed processing {OrderId}", order.OrderId);
        return Ok();
    }

    [HttpPost("RetryShipProducts")]
    public async Task<ActionResult> Post(string orderNumber, string? trackAndTraceNumber)
    {
        var controlPanel = await orderFlows.ControlPanel(orderNumber);
        if (controlPanel is null)
            return NotFound();

        if (trackAndTraceNumber == null)
            await controlPanel.Effects.Remove("ShipProducts");
        else
            await controlPanel.Effects.SetSucceeded("ShipProducts", new TrackAndTrace(trackAndTraceNumber));
        
        await controlPanel.Restart();
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string orderId)
    {
        var controlPanel = await orderFlows.ControlPanel(orderId);
        if (controlPanel is null)
            return NotFound();

        var str = await controlPanel.ToPrettyString();
        return Ok(str);
    }
}
