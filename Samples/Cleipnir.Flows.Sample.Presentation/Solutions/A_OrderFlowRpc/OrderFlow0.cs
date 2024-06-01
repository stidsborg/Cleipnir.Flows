namespace Cleipnir.Flows.Sample.Presentation.Solutions.A_OrderFlowRpc;

public class OrderFlow0 : Flow<Order>
{
    private readonly IPaymentProviderClient _paymentProviderClient;
    private readonly IEmailClient _emailClient;
    private readonly ILogisticsClient _logisticsClient;
    
    public OrderFlow0(IPaymentProviderClient paymentProviderClient, IEmailClient emailClient, ILogisticsClient logisticsClient)
    {
        _paymentProviderClient = paymentProviderClient;
        _emailClient = emailClient;
        _logisticsClient = logisticsClient;
    }
    
    public override async Task Run(Order order)
    {
        var transactionId = Guid.NewGuid();
        await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);

        await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
        
        await _paymentProviderClient.Capture(transactionId);

        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);
    }
}