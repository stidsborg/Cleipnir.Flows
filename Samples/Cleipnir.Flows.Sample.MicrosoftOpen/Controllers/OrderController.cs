using System.Text;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController(OrderFlows orderFlows) : ControllerBase
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
    public async Task<ActionResult> Post(string orderNumber)
    {
        var controlPanel = await orderFlows.ControlPanel(orderNumber);
        if (controlPanel is null)
            return NotFound();

        await controlPanel.Effects.Remove("ShipProducts");
        await controlPanel.Restart();
        
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string orderId)
    {
        var controlPanel = await orderFlows.ControlPanel(orderId);
        if (controlPanel is null)
            return NotFound();

        var effects = controlPanel.Effects;
        var effectIds = await effects.AllIds;

        var stringBuilder = new StringBuilder();
        foreach (var effectId in effectIds)
            stringBuilder.AppendLine(new { Id = effectId, Status = await effects.GetStatus(effectId) }.ToString());

        return Ok(stringBuilder.ToString());    
    }
}
