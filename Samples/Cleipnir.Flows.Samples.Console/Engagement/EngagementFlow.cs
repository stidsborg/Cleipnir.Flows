using Cleipnir.Flows.Reactive;

namespace Cleipnir.Flows.Sample.Console.Engagement;

public class EngagementFlow : Flow<string> 
{
    public override async Task Run(string candidateEmail)
    {
        await DoAtLeastOnce(
            workId: "InitialCorrespondence",
            SendEngagementInitialCorrespondence,
            persistTo: PersistencyMethods.EventSource
        );

        for (var i = 0; i < 10; i++)
        {
            var either = await EventSource
                .OfTypes<EngagementAccepted, EngagementRejected>()
                .Where(either =>
                    either.Match(
                        first: a => a.Iteration == i,
                        second: r => r.Iteration == i
                    )
                )
                .SuspendUntilNext(timeoutEventId: i.ToString(), expiresIn: TimeSpan.FromHours(1));

            var flowCompleted = await either.Match(
                first: async a =>
                {
                    await DoAtLeastOnce(
                        workId: "NotifyHR", 
                        work: () => NotifyHR(candidateEmail),
                        persistTo: PersistencyMethods.EventSource
                    );
                    return true;
                },
                second: r => Task.FromResult(false)
            );

            if (flowCompleted)
                return;
            
            await DoAtLeastOnce(
                workId: $"Reminder#{i}",
                SendEngagementReminder,
                persistTo: PersistencyMethods.EventSource
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