namespace Cleipnir.Flows.Sample.Presentation.Solutions.E_CustomerSignup;

[GenerateFlows]
public class SignupFlow0 : Flow<string>
{
    public override async Task Run(string customerEmail)
    {
        // Flow:
        // 1. send activation mail
        // 2. wait for EmailVerified event or send reminder mail after 1 day
        //    * send a maximum of 3 reminders before failing flow 
        // 3. finally send welcome mail or fail
        
        await SendActivationMail(customerEmail);
        
        for (var i = 0; i <= 3; i++)
        {
            await SendReminderMail(customerEmail);
            
            if (i == 3)
                throw new UserSignupFailedException($"User '{customerEmail}' did not activate within threshold");
        }
        
        await SendWelcomeMail(customerEmail);
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