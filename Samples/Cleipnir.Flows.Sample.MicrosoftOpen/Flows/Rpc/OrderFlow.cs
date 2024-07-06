using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows;

public class OrderFlow : Flow<Order>
{
    private readonly IPaymentProviderClient _paymentProviderClient;
    private readonly IEmailClient _emailClient;
    private readonly ILogisticsClient _logisticsClient;

    public OrderFlow(IPaymentProviderClient paymentProviderClient, IEmailClient emailClient, ILogisticsClient logisticsClient)
    {
        _paymentProviderClient = paymentProviderClient;
        _emailClient = emailClient;
        _logisticsClient = logisticsClient;
    }

    public override async Task Run(Order order)
    {
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid); 
        
        await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);
        await Effect.Capture(
            "ShipProducts",
            () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds),
            ResiliencyLevel.AtMostOnce
        );
        await _paymentProviderClient.Capture(transactionId);
        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);
    }
}

public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);