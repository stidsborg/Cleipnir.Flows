using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc.Solution;

public class OrderFlowWithCleanUp(
    IPaymentProviderClient paymentProviderClient,
    IEmailClient emailClient,
    ILogisticsClient logisticsClient)
    : Flow<Order>
{
   public override async Task Run(Order order)
    {
        var transactionId = Guid.NewGuid(); 
        
        TrackAndTrace? trackAndTrace = null;
        var steps = new StepAndCleanUp[]
        {
            new(
                Work: () => paymentProviderClient.Reserve(transactionId, order.CustomerId, order.TotalPrice),
                CleanUp: () => CleanUp(FailedAt.ProductsShipped, transactionId, trackAndTrace: null)
            ),
            new(
                Work: async () =>
                {
                    trackAndTrace = await Capture(async () =>
                        await logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
                    );
                },
                CleanUp: () => CleanUp(FailedAt.ProductsShipped, transactionId, trackAndTrace: null)
            ),
            new(
                Work: () => paymentProviderClient.Capture(transactionId),
                CleanUp: () => CleanUp(FailedAt.FundsCaptured, transactionId, trackAndTrace)
            ),
            new (
                Work: () => emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace!, order.ProductIds),
                CleanUp: () => CleanUp(FailedAt.OrderConfirmationEmailSent, transactionId, trackAndTrace)
            )
        };
        
        foreach (var step in steps)
            try
            {
                await step.Work();
            }
            catch (Exception)
            {
                await step.CleanUp();
                throw;
            }
    }

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
    }

    private record StepAndCleanUp(Func<Task> Work, Func<Task> CleanUp);
    
    private enum FailedAt
    {
        FundsReserved,
        ProductsShipped,
        FundsCaptured,
        OrderConfirmationEmailSent,
    }
}