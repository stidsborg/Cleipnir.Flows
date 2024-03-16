using Cleipnir.ResilientFunctions.Domain;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Rpc.Solution;

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

        var state = await Effect.CreateOrGet<OrderState>("State");
        await _paymentProviderClient.Reserve(order.CustomerId, state.TransactionId, order.TotalPrice);

        await Effect.Capture(
            "ShipProducts",
            work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
        );

        await _paymentProviderClient.Capture(state.TransactionId);

        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }

    public class OrderState : WorkflowState
    {
        public Guid TransactionId { get; set; } = Guid.NewGuid();
    }
}