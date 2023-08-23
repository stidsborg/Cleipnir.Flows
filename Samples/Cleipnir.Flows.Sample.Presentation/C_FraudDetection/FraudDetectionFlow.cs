using ConsoleApp.FraudDetection;

namespace Cleipnir.Flows.Sample.Presentation.C_FraudDetection;

public class FraudDetectionFlow : Flow<Transaction>
{
    private readonly FraudDetector1 _fraudDetector1;
    private readonly FraudDetector2 _fraudDetector2;
    private readonly FraudDetector3 _fraudDetector3;

    public FraudDetectionFlow(FraudDetector1 fraudDetector1, FraudDetector2 fraudDetector2, FraudDetector3 fraudDetector3)
    {
        _fraudDetector1 = fraudDetector1;
        _fraudDetector2 = fraudDetector2;
        _fraudDetector3 = fraudDetector3;
    }

    public override async Task<bool> Run(Transaction transaction)
    {
        var fraudDetector1 = _fraudDetector1.Approve(transaction, timeout: TimeSpan.FromSeconds(2));
        var fraudDetector2 = _fraudDetector2.Approve(transaction, timeout: TimeSpan.FromSeconds(2));
        var fraudDetector3 = _fraudDetector3.Approve(transaction, timeout: TimeSpan.FromSeconds(2));

        await Task.WhenAll(fraudDetector1, fraudDetector2, fraudDetector3);

        var decisions = new List<bool>();
        if (fraudDetector1.IsCompletedSuccessfully)
            decisions.Add(fraudDetector1.Result);
        if (fraudDetector2.IsCompletedSuccessfully)
            decisions.Add(fraudDetector2.Result);
        if (fraudDetector3.IsCompletedSuccessfully)
            decisions.Add(fraudDetector3.Result);

        return decisions.Count >= 2 && decisions.All(_ => _);
    }
}