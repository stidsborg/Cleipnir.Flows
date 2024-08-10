using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.E_CustomerSignup.Solution;

public class SignupFlow : Flow<string>
{
    public override async Task Run(string customerEmail)
    {
        await Effect.Capture(() => SendActivationMail(customerEmail));
        
        for (var i = 0; i <= 3; i++)
        {
            var emailVerifiedOption = await Messages
                .TakeUntilTimeout(TimeSpan.FromDays(1))
                .OfType<EmailVerified>()
                .FirstOrNone();

            if (emailVerifiedOption.HasValue)
                break;

            if (i == 3)
                throw new UserSignupFailedException($"User '{customerEmail}' did not activate email within threshold");
            
            await Effect.Capture(() => SendReminderMail(customerEmail));
        }

        await Effect.Capture(() => SendWelcomeMail(customerEmail));
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