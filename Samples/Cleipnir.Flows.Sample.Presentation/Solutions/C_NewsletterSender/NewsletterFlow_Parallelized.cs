using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.C_NewsletterSender;

[GenerateFlows]
public class NewsletterFlow_Parallelized : Flow<MailAndRecipients>
{
    public override async Task Run(MailAndRecipients mailAndRecipients)
    {
        var (recipients, subject, content) = mailAndRecipients;
        using var client = new SmtpClient();
        await client.ConnectAsync("mail.smtpbucket.com", 8025);

        await Parallel.ForEachAsync(
            Enumerable.Range(0, recipients.Count),
            async (atRecipient, _) =>
                await Effect.Capture(
                    $"Mail#{atRecipient}",
                    async () =>
                    {
                        var recipient = recipients[atRecipient];
                        var message = new MimeMessage();
                        message.To.Add(new MailboxAddress(recipient.Name, recipient.Address));
                        message.From.Add(new MailboxAddress("Cleipnir.NET", "newsletter@cleipnir.net"));

                        message.Subject = subject;
                        message.Body = new TextPart(TextFormat.Html) { Text = content };
                        await client.SendAsync(message);
                    }
                )
        );
    }
}