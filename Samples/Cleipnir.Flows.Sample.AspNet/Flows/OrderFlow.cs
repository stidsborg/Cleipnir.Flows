using Cleipnir.Flows.Sample.Clients;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Flows;

[GenerateFlows]
public class OrderFlow : Flow<Order>
{
    private readonly IPaymentProviderClient _paymentProviderClient;
    private readonly IEmailClient _emailClient;
    private readonly ILogisticsClient _logisticsClient;
    
    private readonly ILogger _logger = Log.Logger.ForContext<OrderFlow>();

    public OrderFlow(IPaymentProviderClient paymentProviderClient, IEmailClient emailClient, ILogisticsClient logisticsClient)
    {
        _paymentProviderClient = paymentProviderClient;
        _emailClient = emailClient;
        _logisticsClient = logisticsClient;
    }

    public override async Task Run(Order order)
    {
        _logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");
        var transactionId = await Capture(Guid.NewGuid);
        await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);

        await Capture(
            () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
        );

        await _paymentProviderClient.Capture(transactionId);

        await Effect.Capture(
            () => _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds)
        );

        _logger.Information($"Processing of order '{order.OrderId}' completed");
    }
}

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);