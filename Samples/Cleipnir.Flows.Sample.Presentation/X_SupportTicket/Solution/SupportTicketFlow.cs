using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.X_SupportTicket.Solution;

public class SupportTicketFlow : Flow<SupportTicketRequest>
{
    public override async Task Run(SupportTicketRequest request)
    {
        var (supportTicketId, customerSupportAgents) = request;

        for (var i = 0;; i++)
        {
            var customerSupportAgent = customerSupportAgents[i % customerSupportAgents.Length]; 
            await Effect.Capture(
                id: $"RequestSupportForTicket{i}",
                work: () => RequestSupportForTicket(supportTicketId, customerSupportAgent, iteration: i)
            );

            var option = await Messages
                .OfTypes<SupportTicketTaken, SupportTicketRejected>()
                .Where(e => e.Match(taken => taken.Iteration, rejected => rejected.Iteration) == i)
                .TakeUntilTimeout(timeoutEventId: i.ToString(), expiresIn: TimeSpan.FromMinutes(15))
                .SuspendUntilFirstOrNone();

            if (!option.HasValue && option.Value.AsObject() is SupportTicketTaken)
                return; //ticket was taken in iteration i
        }
    }

    private Task RequestSupportForTicket(Guid supportTicketId, string customerSupportAgent, int iteration)
        => MessageBroker.Send(new TakeSupportTicket(supportTicketId, customerSupportAgent, iteration));
}