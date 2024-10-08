using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.Solutions.E_CustomerSignup;

[GenerateFlows]
public class SignupFlow1 : Flow<string>
{
    public override async Task Run(string customerEmail)
    {
        await Effect.Capture("ActivationMail", () => SendActivationMail(customerEmail));
        
        for (var i = 0; i <= 3; i++)
        {
            var emailVerifiedOption = await Messages
                .TakeUntilTimeout($"Timeout_{i}", expiresIn: TimeSpan.FromDays(1))
                .OfType<EmailVerified>()
                .FirstOrNone();

            if (emailVerifiedOption.HasValue)
                break;

            if (i == 3)
                throw new UserSignupFailedException($"User '{customerEmail}' did not activate email within threshold");
            
            await Effect.Capture($"Reminder_{i}", () => SendReminderMail(customerEmail));
        }

        await Effect.Capture("WelcomeMail", () => SendWelcomeMail(customerEmail));
    }

    private static Task SendActivationMail(string customerEmail) => Task.CompletedTask;
    private static Task SendReminderMail(string customerEmail) => Task.CompletedTask;
    private static Task SendWelcomeMail(string customerEmail) => Task.CompletedTask;

    public class UserSignupFailedException : Exception
    {
        public UserSignupFailedException(string? message) : base(message) { }
    }

    public record EmailVerified(string EmailAddress);
}