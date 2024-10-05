using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.PostgreSQL;

namespace Cleipnir.Flows.Sample.Presentation.C_NewsletterSender;

public static class Example
{
    public static async Task Perform()
    {
        var connStr = "Server=localhost;Database=flows;User Id=postgres;Password=Pa55word!; Include Error Detail=true;";
        await DatabaseHelper.CreateDatabaseIfNotExists(connStr);
        var store = new PostgreSqlFunctionStore(connStr);
        await store.Initialize();
        await store.TruncateTables();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<NewsletterFlow>();

        serviceCollection.AddFlows(
            c => c
                .UseStore(store)
                .WithOptions(new Options(unhandledExceptionHandler: Console.WriteLine))
                .RegisterFlowsAutomatically()
        );

        var sp = serviceCollection.BuildServiceProvider();
        var flows = sp.GetRequiredService<NewsletterFlows>();
        
        await flows.Run(
            instanceId: "2023-10",
            new MailAndRecipients(
                [
                    new("Peter Hansen", "peter@gmail.com"),
                    new("Ulla Hansen", "ulla@gmail.com"),
                    new("Heino Hansen", "heino@gmail.com")
                ],
                Subject: "To message queue or not?",
                Content: "Message Queues are omnipresent but do we really need them in our enterprise architectures? ..."
            )
        );
    }
}