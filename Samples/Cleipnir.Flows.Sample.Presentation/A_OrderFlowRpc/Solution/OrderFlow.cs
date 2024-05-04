using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.A_OrderFlowRpc.Solution;

public class OrderFlow : Flow<Order>
{
    private readonly IPaymentProviderClient _paymentProviderClient;
    private readonly IEmailClient _emailClient;
    private readonly ILogisticsClient _logisticsClient;
    
    private ILogger Logger { get; } = Log.Logger.ForContext<OrderFlow>();

    public OrderFlow(IPaymentProviderClient paymentProviderClient, IEmailClient emailClient, ILogisticsClient logisticsClient)
    {
        _paymentProviderClient = paymentProviderClient;
        _emailClient = emailClient;
        _logisticsClient = logisticsClient;
    }
    
    public override async Task Run(Order order)
    {
        Logger.Information($"Processing of order '{order.OrderId}' started");

        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);
        await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);

        try
        {
            await Effect.Capture(
                "ShipProducts",
                work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
            );
        }
        catch (Exception)
        {
            await _paymentProviderClient.CancelReservation(transactionId);
            throw;
        }

        await _paymentProviderClient.Capture(transactionId);

        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }
}