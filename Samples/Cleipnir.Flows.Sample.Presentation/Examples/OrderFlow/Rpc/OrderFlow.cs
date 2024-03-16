using Cleipnir.ResilientFunctions.Domain;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Rpc;

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

        /*
         Clients:
         - PaymentProviderClient
         - LogisticsClient
         - EmailClient
         */

        await Task.CompletedTask;
        Logger.Information($"Processing of order '{order.OrderId}' completed");
    }

    public class OrderScrapbook : WorkflowState
    {
    }
}