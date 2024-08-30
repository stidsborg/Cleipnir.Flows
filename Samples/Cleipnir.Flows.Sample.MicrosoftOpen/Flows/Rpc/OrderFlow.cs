using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc;

public class OrderFlow(
    IPaymentProviderClient paymentProviderClient,
    IEmailClient emailClient,
    ILogisticsClient logisticsClient
) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = Guid.NewGuid();

        await paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);
        var trackAndTrace = await logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
        await paymentProviderClient.Capture(transactionId);
        await emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace, order.ProductIds);
    }
}