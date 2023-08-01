using Cleipnir.Flows.Reactive;

namespace Cleipnir.Flows.Sample.Console.Engagement;

public class EngagementFlow : Flow<string> 
{
    public override async Task Run(string candidateEmail)
    {
        await EventSource.DoAtLeastOnce(
            workId: "InitialCorrespondence",
            SendEngagementInitialCorrespondence
        );
        
        for (var i = 0; i < 10; i++)
        {
            var timeoutOption = await EventSource
                .OfTypes<EngagementAccepted, EngagementRejected>()
                .Where(either =>
                    either.Match(
                        first: a => a.Iteration == i,
                        second: r => r.Iteration == i
                    )
                )
                .SuspendUntilNext(timeoutEventId: i.ToString(), expiresIn: TimeSpan.FromHours(1));

            if (!timeoutOption.TimedOut && timeoutOption.Value!.AsObject() is EngagementAccepted)
            {
                await DoAtLeastOnce(
                    workId: "NotifyHR", 
                    work: () => NotifyHR(candidateEmail),
                    persistTo: PersistencyMedium.EventSource
                );
                return;
            }
            
            await DoAtLeastOnce(
                workId: $"Reminder#{i}",
                SendEngagementReminder,
                persistTo: PersistencyMedium.EventSource
            );
        }

        throw new Exception("Max number of retries exceeded");
    }

    private static Task NotifyHR(string candidateEmail) => Task.CompletedTask;
    private static Task SendEngagementInitialCorrespondence() => Task.CompletedTask;
    private static Task SendEngagementReminder() => Task.CompletedTask;
}

public record EngagementAccepted(int Iteration);
public record EngagementRejected(int Iteration);