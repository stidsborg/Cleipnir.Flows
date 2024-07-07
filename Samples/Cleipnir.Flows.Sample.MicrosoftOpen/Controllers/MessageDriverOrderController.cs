using Cleipnir.Flows.Sample.MicrosoftOpen.Flows;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using static System.Environment;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class MessageDrivenOrderController : ControllerBase
{
    private readonly ILogger _logger = Log.Logger.ForContext<OrderController>();
    private readonly MessageDrivenOrderFlows _orderFlows;

    public MessageDrivenOrderController(MessageDrivenOrderFlows orderFlows)
    {
        _orderFlows = orderFlows;
    }

    [HttpPost]
    public async Task<ActionResult> Post(Order order)
    {
        _logger.Information("Started processing {OrderId}", order.OrderId);
        await _orderFlows.Schedule(order.OrderId, order);
        _logger.Information("Completed processing {OrderId}", order.OrderId);
        
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(string orderId)
    {
        var controlPanel = await _orderFlows.ControlPanel(orderId);
        if (controlPanel is null)
            return NotFound();

        var effects = string.Join(
            NewLine,
            controlPanel
                .Effects
                .All
                .Values
                .Select(se => new { Id = se.EffectId, se.WorkStatus }.ToString())
        );
        
        var messages = string.Join(
            NewLine,
            controlPanel
                .Messages
                .Select(msg => msg.ToString())
        );
        
        return Ok(
            "Effects: " + NewLine + effects + NewLine + NewLine +
            "Messages: " + NewLine + messages + NewLine
        );
    }
}
