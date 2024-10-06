﻿using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;
using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc.Solution;

[GenerateFlows]
public class OrderFlow(
    IPaymentProviderClient paymentProviderClient,
    IEmailClient emailClient,
    ILogisticsClient logisticsClient)
    : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid); 
        
        await paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);
        var trackAndTrace = await Effect.Capture(
            "ShipProducts",
            () => logisticsClient.ShipProducts(order.CustomerId, order.ProductIds),
            ResiliencyLevel.AtMostOnce
        );
        await paymentProviderClient.Capture(transactionId);
        await emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace, order.ProductIds);
    }
}