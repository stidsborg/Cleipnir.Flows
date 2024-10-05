using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.C_NewsletterSender.Distributed;

public class NewsletterParentFlow(NewsletterChildFlows childFlows) : Flow<MailAndRecipients>
{
    public override async Task Run(MailAndRecipients param)
    {
        Console.WriteLine("Started NewsletterParentFlow");
        
        var (recipients, subject, content) = param;

        var bulkWork = recipients
            .Split(3)
            .Select(emails => new MailAndRecipients(emails, subject, content))
            .Select((mailAndRecipients, child) => new NewsletterChildWork(child, mailAndRecipients, Workflow.FlowId))
            .Select(work =>
                new BulkWork<NewsletterChildWork>(
                    Instance: $"{Workflow.FlowId.Instance}_Child{work.Child}",
                    work
                )
            );
        
        await childFlows.BulkSchedule(bulkWork);

        await Messages
            .OfType<EmailsSent>()
            .Take(3)
            .Completion(maxWait: TimeSpan.FromMinutes(30));
        
        Console.WriteLine("Finished NewsletterParentFlow");
    }
    
    public record EmailsSent(int Child);
}