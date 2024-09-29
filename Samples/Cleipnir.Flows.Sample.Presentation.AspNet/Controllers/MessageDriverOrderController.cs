using System.Text;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;
using Cleipnir.ResilientFunctions.Helpers;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using static System.Environment;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class MessageDrivenOrderController(MessageDrivenOrderFlows orderFlows) : ControllerBase
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

        var effectIds = await controlPanel.Effects.AllIds;
        var effectsBuilder = new StringBuilder();
        foreach (var effectId in effectIds)
            effectsBuilder.AppendLine(
                new { Id = effectId, Status = await controlPanel.Effects.GetStatus(effectId) }.ToString()
            );
        
        var messages = string.Join(
            NewLine,
            await controlPanel
                .Messages
                .AsObjects
                .SelectAsync(msg => msg.ToString())
        );
        
        return Ok(
            "Effects: " + NewLine + effectsBuilder + NewLine + NewLine +
            "Messages: " + NewLine + messages + NewLine
        );
    }
}
