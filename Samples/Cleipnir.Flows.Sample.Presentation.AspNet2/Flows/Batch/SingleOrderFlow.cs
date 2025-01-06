using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Batch;

[GenerateFlows]
public class SingleOrderFlow(
    ILogger<SingleOrderFlow> logger,
    IPaymentProviderClient paymentProviderClient,
    IEmailClient emailClient,
    ILogisticsClient logisticsClient
) : Flow<Order, TransactionIdAndTrackAndTrace>
{
    public override async Task<TransactionIdAndTrackAndTrace> Run(Order order)
    {
        logger.LogInformation("{OrderId}: Started processing order", order.OrderId);
        var transactionId = await Capture(Guid.NewGuid);

        await paymentProviderClient.Reserve(transactionId, order.CustomerId, order.TotalPrice);
        var trackAndTrace = await Capture(() => logisticsClient.ShipProducts(order.CustomerId, order.ProductIds));
        await paymentProviderClient.Capture(transactionId);
        await emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace, order.ProductIds);
        logger.LogInformation("{OrderId}: Completed processing order", order.OrderId);

        return new TransactionIdAndTrackAndTrace(order.OrderId, transactionId, trackAndTrace);
    }
}