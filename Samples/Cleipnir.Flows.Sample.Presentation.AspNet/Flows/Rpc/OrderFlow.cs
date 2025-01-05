using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;
using Polly;
using Polly.Retry;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc;

[GenerateFlows]
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

    #region CleanUp

    private async Task CleanUp(FailedAt failedAt, Guid transactionId, TrackAndTrace? trackAndTrace)
    {
        switch (failedAt) 
        {
            case FailedAt.FundsReserved:
                break;
            case FailedAt.ProductsShipped:
                await paymentProviderClient.CancelReservation(transactionId);
                break;
            case FailedAt.FundsCaptured:
                await paymentProviderClient.Reverse(transactionId);
                await logisticsClient.CancelShipment(trackAndTrace!);
                break;
            case FailedAt.OrderConfirmationEmailSent:
                //we accept this failure without cleaning up
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(failedAt), failedAt, null);
        }

        throw new OrderProcessingException($"Order processing failed at: '{failedAt}'");
    }

    private record StepAndCleanUp(Func<Task> Work, Func<Task> CleanUp);
    
    private enum FailedAt
    {
        FundsReserved,
        ProductsShipped,
        FundsCaptured,
        OrderConfirmationEmailSent,
    }

    #endregion
}