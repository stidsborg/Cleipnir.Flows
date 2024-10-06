using Cleipnir.ResilientFunctions.Domain;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace Cleipnir.Flows.Sample.Presentation.C_NewsletterSender.Distributed;

[GenerateFlows]
public class NewsletterChildFlow(NewsletterParentFlows parentFlows, int child) : Flow<NewsletterChildWork>
{
    public override async Task Run(NewsletterChildWork work)
    {
        Console.WriteLine($"Starting child: {child}");
        
        var (recipients, subject, content) = work.MailAndRecipients;
        using var client = new SmtpClient();
        await client.ConnectAsync("mail.smtpbucket.com", 8025);
        
        for (var index = 0; index < recipients.Count; index++)
        {
            var recipient = recipients[index];
            var message = new MimeMessage();
            message.To.Add(new MailboxAddress(recipient.Name, recipient.Address));
            message.From.Add(new MailboxAddress("Cleipnir.NET", "newsletter@cleipnir.net"));

            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = content };
            await client.SendAsync(message);
        }

        await parentFlows.SendMessage(
            work.Parent.Instance,
            new NewsletterParentFlow.EmailsSent(work.Child),
            idempotencyKey: work.Child.ToString()
        );
        
        Console.WriteLine($"Finishing child: {child}");
    }
}

public record NewsletterChildWork(int Child, MailAndRecipients MailAndRecipients, FlowId Parent);