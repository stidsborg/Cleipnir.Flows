using Cleipnir.ResilientFunctions.Domain;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace Cleipnir.Flows.Sample.Presentation.A_NewsletterSender.Solution;

public class NewsletterFlow : Flow<MailAndRecipients>
{
    public override async Task Run(MailAndRecipients mailAndRecipients)
    {
        var (recipients, subject, content) = mailAndRecipients;
        using var client = new SmtpClient();
        await client.ConnectAsync("mail.smtpbucket.com", 8025);

        var state = await Effect.CreateOrGet<State>(nameof(State));
        
        for (var atRecipient = state.AtRecipient; atRecipient < mailAndRecipients.Recipients.Count; atRecipient++)
        {
            var recipient = recipients[atRecipient];
            var message = new MimeMessage();
            message.To.Add(new MailboxAddress(recipient.Name, recipient.Address));
            message.From.Add(new MailboxAddress("Cleipnir.NET", "newsletter@cleipnir.net"));

            message.Subject = subject;
            message.Body = new TextPart(TextFormat.Html) { Text = content };
            await client.SendAsync(message);

            state.AtRecipient = atRecipient;
            await state.Save();
        }
    }
    
    private class State : WorkflowState
    {
        public int AtRecipient { get; set; }
    }
}

public record EmailAddress(string Name, string Address);
public record MailAndRecipients(
    List<EmailAddress> Recipients,
    string Subject,
    string Content
);