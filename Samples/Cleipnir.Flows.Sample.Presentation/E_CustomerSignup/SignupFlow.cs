namespace Cleipnir.Flows.Sample.Presentation.E_CustomerSignup;

public class SignupFlow : Flow<string>
{
    public override async Task Run(string customerEmail)
    {
        // Flow:
        // 1. send activation mail
        // 2. wait for EmailVerified event or send reminder mail
        // 3. finally send welcome mail or fail
        
        await SendActivationMail(customerEmail);
        await SendReminderMail(customerEmail);
        await SendWelcomeMail(customerEmail);
        
        for (var i = 0; i <= 5; i++)
        {
            if (i == 5)
                throw new UserSignupFailedException($"User '{customerEmail}' did not activate within threshold");
        }
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