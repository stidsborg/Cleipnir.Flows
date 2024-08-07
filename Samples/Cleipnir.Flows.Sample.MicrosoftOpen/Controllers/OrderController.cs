using Cleipnir.Flows.Sample.MicrosoftOpen.Flows;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

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
    
    [HttpPost("RetryShipProducts")]
    public async Task<ActionResult> Post(string orderNumber)
    {
        var controlPanel = await _orderFlows.ControlPanel(orderNumber);
        if (controlPanel is null)
            return NotFound();

        await controlPanel.Effects.Remove("ShipProducts");
        await controlPanel.Restart();
        
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string orderId)
    {
        var controlPanel = await _orderFlows.ControlPanel(orderId);
        if (controlPanel is null)
            return NotFound();

        var effects = string.Join(
            Environment.NewLine,
            controlPanel
                .Effects
                .All
                .Values
                .Select(se => new { Id = se.EffectId, se.WorkStatus }.ToString())
        );
        
        return Ok(effects);
    }
}
