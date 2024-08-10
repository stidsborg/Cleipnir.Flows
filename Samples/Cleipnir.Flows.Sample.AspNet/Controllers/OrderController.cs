using System.Text;
using Cleipnir.Flows.Sample.Flows;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly ILogger _logger = Log.Logger.ForContext<OrderController>();
    private readonly OrderFlows _orderFlows;

    public OrderController(OrderFlows orderFlows)
    {
        _orderFlows = orderFlows;
    }

    [HttpPost]
    public async Task<ActionResult> Post(Order order)
    {
        _logger.Information("Started processing {OrderId}", order.OrderId);
        await _orderFlows.Run(order.OrderId, order);
        _logger.Information("Completed processing {OrderId}", order.OrderId);
        
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string orderId)
    {
        var controlPanel = await _orderFlows.ControlPanel(orderId);
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
