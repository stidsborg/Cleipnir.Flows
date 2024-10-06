using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.Presentation.F_SmsVerificationFlow;

[GenerateFlows]
public class SmsFlow : Flow<string>, IExposeState<SmsFlow.SmsState>
{
    public required SmsState State { get; init; }
    
    public override async Task Run(string customerPhoneNumber)
    {
        for (var i = 0; i < 5; i++)
        {
            var generatedCode = await Effect.Capture(
                $"SendSms#{i}",
                async () =>
                {
                    var generatedCode = GenerateOneTimeCode();
                    await SendSms(customerPhoneNumber, generatedCode);
                    return generatedCode;
                }
            );
            
            var codeFromUser = await Messages
                .OfType<CodeFromUser>()
                .Skip(i)
                .First();

            if (IsExpired(codeFromUser))
                State.Status = MostRecentAttempt.CodeExpired;
            else if (codeFromUser.Code == generatedCode)
            {
                State.Status = MostRecentAttempt.Success;
                return;
            }

            State.Status = MostRecentAttempt.IncorrectCode;
        }

        State.Status = MostRecentAttempt.MaxAttemptsExceeded;
    }

    public class SmsState : FlowState
    {
        public MostRecentAttempt Status { get; set; }
    }
    
    public record CodeFromUser(string CustomerPhoneNumber, string Code, DateTime Timestamp);

    public enum MostRecentAttempt
    {
        NotStarted,
        CodeExpired,
        IncorrectCode,
        Success,
        MaxAttemptsExceeded
    }
    
    private string GenerateOneTimeCode()
    {
        throw new NotImplementedException();
    }

    private Task SendSms(string customerPhoneNumber, string generatedCode)
    {
        throw new NotImplementedException();
    }

    private bool IsExpired(CodeFromUser code)
    {
        throw new NotImplementedException();
    }
}