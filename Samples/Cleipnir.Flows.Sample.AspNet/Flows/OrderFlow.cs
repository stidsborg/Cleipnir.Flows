using Cleipnir.Flows.Sample.Clients;
using Cleipnir.ResilientFunctions.Domain;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Cleipnir.Flows.Sample.Flows;

public class OrderFlow : Flow<Order, OrderScrapbook>
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
        
        await _paymentProviderClient.Reserve(order.CustomerId, Scrapbook.TransactionId, order.TotalPrice);

        await DoAtMostOnce(
            workStatus: scrapbook => scrapbook.ProductsShipped,
            () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
        );

        await _paymentProviderClient.Capture(Scrapbook.TransactionId);

        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

        _logger.Information($"Processing of order '{order.OrderId}' completed");
    }
}

public class OrderScrapbook : RScrapbook
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();
    public WorkStatus ProductsShipped { get; set; }
}
public record Order(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds, decimal TotalPrice);