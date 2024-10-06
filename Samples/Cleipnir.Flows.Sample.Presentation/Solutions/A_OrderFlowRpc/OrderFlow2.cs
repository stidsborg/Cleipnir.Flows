using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.A_OrderFlowRpc;

[GenerateFlows]
public class OrderFlow2 : Flow<Order>
{
    private readonly IPaymentProviderClient _paymentProviderClient;
    private readonly IEmailClient _emailClient;
    private readonly ILogisticsClient _logisticsClient;

    public OrderFlow2(IPaymentProviderClient paymentProviderClient, IEmailClient emailClient, ILogisticsClient logisticsClient)
    {
        _paymentProviderClient = paymentProviderClient;
        _emailClient = emailClient;
        _logisticsClient = logisticsClient;
    }
    
    public override async Task Run(Order order)
    {
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);
        await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);

        try
        {
            await Effect.Capture(
                "ShipProducts",
                work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds),
                ResiliencyLevel.AtMostOnce
            );
        }
        catch (Exception)
        {
            await _paymentProviderClient.CancelReservation(transactionId);
            throw;
        }

        await _paymentProviderClient.Capture(transactionId);

        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);
    }
}