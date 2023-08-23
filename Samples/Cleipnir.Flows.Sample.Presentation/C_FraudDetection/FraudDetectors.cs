using Cleipnir.ResilientFunctions.Helpers;
using ConsoleApp.FraudDetection;

namespace Cleipnir.Flows.Sample.Presentation.C_FraudDetection;

public interface IFraudDetector
{
    Task<bool> Approve(Transaction transaction, TimeSpan timeout);
}

public class FraudDetector1 : IFraudDetector
{
    public Task<bool> Approve(Transaction transaction, TimeSpan timeout) => true.ToTask();
}

public class FraudDetector2 : IFraudDetector
{
    public Task<bool> Approve(Transaction transaction, TimeSpan timeout) => true.ToTask();
}

public class FraudDetector3 : IFraudDetector
{
    public Task<bool> Approve(Transaction transaction, TimeSpan timeout) => true.ToTask();
}