using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.B_CustomerSignup.Solution;

public class SignupFlow : Flow<string>
{
    public override async Task Run(string customerEmail)
    {
        await Effect.Capture("ActivationMail", () => SendActivationMail(customerEmail));

        for (var i = 0; i <= 5; i++)
        {
            var emailVerifiedOption = await Messages
                .OfType<EmailVerified>()
                .TakeUntilTimeout($"Timeout_{i}", TimeSpan.FromDays(1))
                .SuspendUntilFirstOrNone();

            if (emailVerifiedOption.HasValue)
                break;

            if (i == 5)
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