﻿using Cleipnir.ResilientFunctions.Domain;
using Serilog;

namespace Cleipnir.Flows.Sample.Presentation.Examples.OrderFlow.Rpc;

public class OrderFlow : Flow<Order, OrderFlow.OrderScrapbook>
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
        Log.Logger.ForContext<OrderFlow>().Information($"Processing of order '{order.OrderId}' started");

        await _paymentProviderClient.Reserve(order.CustomerId, Scrapbook.TransactionId, order.TotalPrice);

        await Scrapbook.DoAtMostOnce(
            workStatus: s => s.ProductsShippedStatus,
            work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
        );

        await _paymentProviderClient.Capture(Scrapbook.TransactionId);

        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

        Log.Logger.ForContext<OrderFlow>().Information($"Processing of order '{order.OrderId}' completed");
    }

    public class OrderScrapbook : RScrapbook
    {
        public Guid TransactionId { get; set; } = Guid.NewGuid();
        public WorkStatus ProductsShippedStatus { get; set; }
    }
}