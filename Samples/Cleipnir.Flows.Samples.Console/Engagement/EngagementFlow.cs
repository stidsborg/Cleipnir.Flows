using Cleipnir.ResilientFunctions.Reactive.Extensions;

namespace Cleipnir.Flows.Sample.ConsoleApp.Engagement;

public class EngagementFlow : Flow<string> 
{
    public override async Task Run(string candidateEmail)
    {
        await Effect.Capture(
            id: "InitialCorrespondence",
            SendEngagementInitialCorrespondence
        );
        
        for (var i = 0; i < 10; i++)
        {
            var either = await Messages
                .TakeUntilTimeout($"Timeout_{i}", expiresIn: TimeSpan.FromHours(1))
                .OfTypes<EngagementAccepted, EngagementRejected>()
                .Where(either =>
                    either.Match(
                        first: a => a.Iteration == i,
                        second: r => r.Iteration == i
                    )
                )
                .FirstOrDefault();

            if (either?.AsObject() is EngagementAccepted)
            {
                await Effect.Capture(
                    id: "NotifyHR", 
                    work: () => NotifyHR(candidateEmail)
                );
                return;
            }
            
            await Effect.Capture(
                id: $"Reminder#{i}",
                SendEngagementReminder
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