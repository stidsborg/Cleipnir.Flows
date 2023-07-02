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
}
