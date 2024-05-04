namespace Cleipnir.Flows.Sample.Presentation.D_LoanApplication;

public static class CreditChecker1
{
    public static void Start()
    {
        MessageBroker.Subscribe(events =>
        {
            if (events is PerformCreditCheck command) 
                _ = Approve(command);

            return Task.CompletedTask;
        });
    }

    private static async Task Approve(PerformCreditCheck loanApplication)
    {
        await Task.Delay(10);
        _ = MessageBroker.Send(
            new CreditCheckOutcome(nameof(CreditChecker2), loanApplication.Id, Approved: true)
        );
    } 
}

public class CreditChecker2 
{
    public static void Start()
    {
        MessageBroker.Subscribe(events =>
        {
            if (events is PerformCreditCheck command) 
                _ = Approve(command);
 
            return Task.CompletedTask;
        });
    }

    private static async Task Approve(PerformCreditCheck loanApplication)
    {
        await Task.Delay(10);
        _ = MessageBroker.Send(
            new CreditCheckOutcome(nameof(CreditChecker2), loanApplication.Id, Approved: true)
        );
    } 
}

public class CreditChecker3 
{
    public static void Start()
    {
        MessageBroker.Subscribe(events =>
        {
            if (events is PerformCreditCheck command) 
                _ = Approve(command);

            return Task.CompletedTask;
        });
    }

    private static async Task Approve(PerformCreditCheck loanApplicationCommand)
    {
        await Task.Delay(10);
        _ = MessageBroker.Send(
            new CreditCheckOutcome(nameof(CreditChecker3), loanApplicationCommand.Id, Approved: true)
        );
    } 
}