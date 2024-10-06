using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;
using Polly;
using Polly.Retry;

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

        await paymentProviderClient.Reserve(transactionId, order.CustomerId, order.TotalPrice);
        var trackAndTrace = await logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
        await paymentProviderClient.Capture(transactionId);
        await emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace, order.ProductIds);
    }

    #region Polly

    private ResiliencePipeline Pipeline { get; } = new ResiliencePipelineBuilder()
        .AddRetry(
            new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // Adds a random factor to the delay
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(3),
            }
        ).Build();

    #endregion
}