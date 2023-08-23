using Cleipnir.Flows.PostgresSql;

namespace Cleipnir.Flows.Sample.Presentation.A_NewsletterSender;

public static class Example
{
    public static async Task Perform()
    {
        var connStr = "Server=localhost;Database=flows;User Id=postgres;Password=Pa55word!; Include Error Detail=true;";
        var flowStore = new PostgresFlowStore(connStr);
        await flowStore.DropIfExists();
        await flowStore.Initialize();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddTransient<NewsletterFlow>();
        var flowsContainer = new FlowsContainer(
            flowStore,
            serviceCollection.BuildServiceProvider(),
            new Options(unhandledExceptionHandler: Console.WriteLine)
        );
        
        var flows = new NewsletterFlows(flowsContainer);
        await flows.Run(
            "2023-10",
            new MailAndRecipients(
                new List<EmailAddress>
                {
                    new("Peter Hansen", "peter@gmail.com"),
                    new("Ulla Hansen", "ulla@gmail.com"),
                    new("Heino Hansen", "heino@gmail.com")
                },
                Subject: "To message queue or not?",
                Content: "Message Queues are omnipresent but do we really need them in our enterprise architectures? ..."
            )
        );
    }
}