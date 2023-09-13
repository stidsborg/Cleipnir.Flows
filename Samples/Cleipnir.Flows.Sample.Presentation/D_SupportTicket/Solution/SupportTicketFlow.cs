using Cleipnir.Flows.Reactive;
using Cleipnir.ResilientFunctions.Domain;

namespace Cleipnir.Flows.Sample.Presentation.D_SupportTicket.Solution;

public class SupportTicketFlow : Flow<SupportTicketRequest>
{
    public override async Task Run(SupportTicketRequest request)
    {
        var (supportTicketId, customerSupportAgents) = request;

        for (var i = 0;; i++)
        {
            var customerSupportAgent = customerSupportAgents[i % customerSupportAgents.Length]; 
            await Scrapbook.DoAtLeastOnce(
                workId: $"RequestSupportForTicket{i}",
                work: () => RequestSupportForTicket(supportTicketId, customerSupportAgent, iteration: i)
            );

            var option = await EventSource
                .OfTypes<SupportTicketTaken, SupportTicketRejected>()
                .Where(e => e.Match(taken => taken.Iteration, rejected => rejected.Iteration) == i)
                .SuspendUntilNext(timeoutEventId: i.ToString(), expiresIn: TimeSpan.FromMinutes(15));

            if (!option.TimedOut && option.Value!.AsObject() is SupportTicketTaken)
                return; //ticket was taken in iteration i
        }
    }

    private Task RequestSupportForTicket(Guid supportTicketId, string customerSupportAgent, int iteration)
        => MessageBroker.Send(new TakeSupportTicket(supportTicketId, customerSupportAgent, iteration));
}