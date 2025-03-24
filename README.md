[![.NET](https://github.com/stidsborg/Cleipnir.Flows/actions/workflows/dotnet.yml/badge.svg?no-cache)](https://github.com/stidsborg/Cleipnir.Flows/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/dt/Cleipnir.Flows.svg)](https://www.nuget.org/packages/Cleipnir.Flows)
[![NuGet](https://img.shields.io/nuget/vpre/Cleipnir.Flows.svg)](https://www.nuget.org/packages/Cleipnir.Flows)
[![alt Join the conversation](https://img.shields.io/discord/1330489830299402360.svg?no-cache "Discord")](https://discord.gg/JzSzaNfus2)

<p align="center">
  <img src="https://github.com/stidsborg/Cleipnir.Flows/blob/main/Docs/cleipnir.png" alt="logo" />
  <br>
  Simply making <strong>crash-resilient</strong> code <strong>simple</strong>
  <br>
</p>

# Cleipnir.NET
Cleipnir Flows is a powerful **durable execution** .NET framework - ensuring your code will always execute to completetion **correctly**.
* Makes C#-code behave correctly after a crash, restart or suspension
* Wait for external events directly inside your code
* Suspend code execution for minutes, hours, weeks or longer
* Requires only a database 
* Use with ASP.NET / generic host service
* Integrates easily with all message-brokers and service-buses
* Removes need for **saga-pattern** and **outbox-pattern**
* Powerful alterrnative to job-schedulers (HangFire, Quartz)

## Abstractions
Cleipnir.NET provides the following 3 abstractions:
### Capture
Remembers the result of arbitary code:
```csharp
var transactionId = await Capture("TransactionId", () => Guid.NewGuid());
//or simply
var transactionId = await Capture(Guid.NewGuid);
```
### Messages
Wait for retrival of external message - without taking up resources: 
```csharp
var fundsReserved = await Messages<FundsReserved>(timesOutIn: TimeSpan.FromMinutes(5));
```
### Suspension 
Suspends the current execution at-will, resuming after some duration: 
```csharp
await Delay(TimeSpan.FromMinutes(5));
```
## Examples
### Message-brokered ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/main/Source/Flows/Ordering/MessageDriven/MessageDrivenOrderFlow.cs)):
```csharp
[GenerateFlows]
public class OrderFlow(IBus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = await Capture(Guid.NewGuid); //generated transaction id is fixed after this statement

        await PublishReserveFunds(order, transactionId);
        await Message<FundsReserved>(); //execution is suspended until a funds reserved message is received
        
        await PublishShipProducts(order);
        var trackAndTraceNumber = (await Message<ProductsShipped>()).TrackAndTraceNumber; 
        
        await PublishCaptureFunds(order, transactionId);
        await Message<FundsCaptured>();
        
        await PublishSendOrderConfirmationEmail(order, trackAndTraceNumber);
        await Message<OrderConfirmationEmailSent>();
    }

    private Task PublishReserveFunds(Order order, Guid transactionId) 
        => Capture(async () => await bus.Publish(new ReserveFunds(order.OrderId, order.TotalPrice, transactionId, order.CustomerId)));
}
```

### RPC ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/main/Source/Flows/Ordering/Rpc/OrderFlow.cs)):
```csharp
[GenerateFlows]
public class OrderFlow(
    IPaymentProviderClient paymentProviderClient,
    IEmailClient emailClient,
    ILogisticsClient logisticsClient
) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = Capture(() => Guid.NewGuid); //generated transaction id is fixed after this statement

        await paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);
        var trackAndTrace = await Capture( 
            () => paymentProviderClient.Capture(transactionId),
            ResiliencyLevel.AtMostOnce
        ); //external calls can also be captured - will never be called multiple times

        await emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace, order.ProductIds);
    }
}
```

## What is durable execution?
Durable execution is an emerging paradigm for simplifying the implementation of code which can safely resume execution after a process crash or restart (i.e. after a production deployment).
It allows the developer to implement such code using ordinary C#-code with loops, conditionals and so on.

Furthermore, durable execution allows suspending code execution for an arbitraty amount of time - thereby saving process resources.

Essentially, durable execution works by saving state at explicitly defined points during the invocation of code, thereby allowing the framework to skip over previously executed parts of the code when/if the code is re-executed. This occurs both after a crash and suspension.

## Why durable execution?
Currently, implementing resilient business flows either entails (1) sagas (i.e. MassTransit, NServiceBus) or (2) job-schedulers (i.e. HangFire).

Both approaches have a unique set of challenges:
* Saga - becomes difficult to implement for real-world scenarios as they are either realized by declaratively constructing a state-machine or implementing a distinct message handler per message type.
* Job-scheduler - requires one to implement idempotent code by hand (in case of failure). Moreover, it cannot be integrated with message-brokers and does not support programmatically suspending a job in the middle of its execution.

## Getting Started
To get started simply perform the following three steps in an ASP.NET or generic-hosted service ([sample repo](https://github.com/stidsborg/Cleipnir.Flows.Sample/)):

Firstly, install the Cleipnir.Flows nuget package (using either Postgres, SqlServer or MariaDB as persistence layer). I.e.
```powershell
Install-Package Cleipnir.Flows.Postgres
```

Secondly, add the following to the setup in `Program.cs` ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/d0c0584edf796db7202e61592b0cc2fd5f1ea909/Program.cs#L17)):
```csharp
builder.Services.AddFlows(c => c
  .UsePostgresSqlStore(connectionString)  
  .RegisterFlowsAutomatically()
);
```

### RPC Flows
Finally, implement your flow ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/main/Source/Flows/Ordering/Rpc/OrderFlow.cs)):
```csharp
[GenerateFlows]
public class OrderFlow(
    IPaymentProviderClient paymentProviderClient,
    IEmailClient emailClient,
    ILogisticsClient logisticsClient
) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = Capture(() => Guid.NewGuid);

        await paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);
        var trackAndTrace = await Capture(
            () => paymentProviderClient.Capture(transactionId),
            ResiliencyLevel.AtMostOnce
        );

        await emailClient.SendOrderConfirmation(order.CustomerId, trackAndTrace, order.ProductIds);
    }
}
```

### Message-brokered Flows
Or, if the flow is using a message-broker ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/main/Source/Flows/Ordering/MessageDriven/MessageDrivenOrderFlow.cs)):
```csharp
[GenerateFlows]
public class OrderFlow(IBus bus) : Flow<Order>
{
    public override async Task Run(Order order)
    {
        var transactionId = await Capture(Guid.NewGuid);

        await PublishReserveFunds(order, transactionId);
        await Message<FundsReserved>();
        
        await PublishShipProducts(order);
        var trackAndTraceNumber = (await Message<ProductsShipped>()).TrackAndTraceNumber;
        
        await PublishCaptureFunds(order, transactionId);
        await Message<FundsCaptured>();
        
        await PublishSendOrderConfirmationEmail(order, trackAndTraceNumber);
        await Message<OrderConfirmationEmailSent>();
    }
```

The implemented flow can then be started using the corresponding source generated Flows-type ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/main/Source/Flows/Ordering/Rpc/OrderController.cs)):
```csharp
[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderFlows _orderFlows;

    public OrderController(OrderFlows orderFlows) => _orderFlows = orderFlows;

    [HttpPost]
    public async Task Post(Order order) => await _orderFlows.Run(order.OrderId, order);
}
```

## Media
[![Video link](https://img.youtube.com/vi/ID-bz6PUWF8/0.jpg)](https://www.youtube.com/watch?v=ID-bz6PUWF8)&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;[![Video link](https://img.youtube.com/vi/GDgSbcOFsdY/0.jpg)](https://www.youtube.com/watch?v=GDgSbcOFsdY)

## Discord
Get live help at the Discord channel:

[![alt Join the conversation](https://img.shields.io/discord/1330489830299402360.svg?no-cache "Discord")](https://discord.gg/JzSzaNfus2)

## Service Bus Integrations
It is simple to use Cleipnir with all the popular service bus frameworks. In order to do simply implement an event handler - which forwards received events - for each flow type:

### MassTransit Handler
```csharp
public class SimpleFlowsHandler(SimpleFlows simpleFlows) : IConsumer<MyMessage>
{
    public Task Consume(ConsumeContext<MyMessage> context) 
        => simpleFlows.SendMessage(context.Message.Value, context.Message);
}
```
[Source code](https://github.com/stidsborg/Cleipnir.Flows/blob/main/ServiceBuses/MassTransit/Cleipnir.Flows.MassTransit.Console/SimpleFlow.cs)

### NServiceBus Handler
```csharp
public class SimpleFlowsHandler(SimpleFlows flows) : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage message, IMessageHandlerContext context)
        => flows.SendMessage(message.Value, message);
}
```
[Source code](https://github.com/stidsborg/Cleipnir.Flows/blob/main/ServiceBuses/NServiceBus/Cleipnir.Flows.NServiceBus.Console/SimpleFlow.cs)

### Rebus Handler
```csharp
public class SimpleFlowsHandler(SimpleFlows simpleFlows) : IHandleMessages<MyMessage>
{
    public Task Handle(MyMessage msg) => simpleFlows.SendMessage(msg.Value, msg);
}
```
[Source code](https://github.com/stidsborg/Cleipnir.Flows/blob/main/ServiceBuses/Rebus/Cleipnir.Flows.Rebus.Console/SimpleFlow.cs)

### Wolverine Handler
```csharp
public class SimpleFlowsHandler(SimpleFlows flows)
{
    public Task Handle(MyMessage myMessage)
        => flows.SendMessage(myMessage.Value, myMessage);
}
```
[Source code](https://github.com/stidsborg/Cleipnir.Flows/blob/main/ServiceBuses/Wolverine/Cleipnir.Flows.Wolverine.Console/SimpleFlow.cs)

### Publish multiple messages in batch

```csharp
await flows.SendMessages(batchedMessages);
```
[Source code](https://github.com/stidsborg/Cleipnir.Flows/blob/340c4b2830a047523b95ef443b39ea9f3ce9c97f/Cleipnir.Flows.Tests.AspNet/BulkPublishTests.cs#L37)

### Kafka Handler

```csharp
ConsumeMessages(
  batchSize: 10,
  topic,
  handler: messages =>
    flows.SendMessages(
      messages.Select(msg => new BatchedMessage(msg.Instance, msg)).ToList()
));
```
[Source code](https://github.com/stidsborg/Cleipnir.Flows/blob/76d65bf5ff66275b949bdd32c4797d45dbe7396a/ServiceBuses/Kafka/Cleipnir.Flows.Kafka.Tests/MessageTests.cs#L24)

## More examples
As an example is worth a thousand lines of documentation - various useful examples are presented in the following section:

### Integration-test ([source code](https://github.com/stidsborg/Cleipnir.Flows.Sample/blob/48eceea40402590303d5a269aeec9912117c634c/Tests/Cleipnir.Flows.Sample.Tests/RpcOrderFlowTests.cs#L16)):
```csharp
var transactionId = Guid.NewGuid();
var usedTransactionId = default(Guid?);
        
var serviceProvider = new ServiceCollection()
  .AddSingleton(new OrderFlow(
    PaymentProviderClientTestStub.Create(reserve: (id, _, _) => { usedTransactionId = id; return Task.CompletedTask; }),
    EmailClientStub.Instance, 
    LogisticsClientStub.Instance)
  ).BuildServiceProvider();

using var container = FlowsContainer.Create(serviceProvider);
var flows = new OrderFlows(container);
        
var testOrder = new Order("MK-54321", CustomerId: Guid.NewGuid(), ProductIds: [Guid.NewGuid()], TotalPrice: 120);
          
await flows.Run(
  instanceId: testOrder.OrderId,
  testOrder,
  new InitialState(Messages: [], Effects: [new InitialEffect("TransactionId", transactionId)])
);

Assert.AreEqual(transactionId, usedTransactionId);
```


### Avoid re-executing already completed code:
```csharp
[GenerateFlows]
public class AtLeastOnceFlow : Flow<string, string>
{
  private readonly PuzzleSolverService _puzzleSolverService = new();

  public override async Task<string> Run(string hashCode)
  {
    var solution = await Effect.Capture(
      id: "PuzzleSolution",
      work: () => _puzzleSolverService.SolveCryptographicPuzzle(hashCode)
    );

    return solution;
  }
}
```

### Ensure code is **executed at-most-once**:
```csharp
[GenerateFlows]
public class AtMostOnceFlow : Flow<string>
{
    private readonly RocketSender _rocketSender = new();
    
    public override async Task Run(string rocketId)
    {
        await Effect.Capture(
            id: "FireRocket",
            _rocketSender.FireRocket,
            ResiliencyLevel.AtMostOnce
        );
    }
}
```

### Wait for 2 external messages before continuing flow ([source code](https://github.com/stidsborg/Cleipnir.Flows/blob/main/Samples/Cleipnir.Flows.Samples.Console/WaitForMessages/WaitForMessagesFlow.cs)):
```csharp
[GenerateFlows]
public class WaitForMessagesFlow : Flow<string>
{
  public override async Task Run(string param)
  {
    await Messages
      .OfTypes<FundsReserved, InventoryLocked>()
      .Take(2)
      .Completion(maxWait: TimeSpan.FromSeconds(30));

    System.Console.WriteLine("Complete order-processing");
  }
}
```
When the max wait duration has passed the flow is automatically suspended in order to save resources.
Thus, the flow can also be suspended immediately when all messages have not been received:
```csharp
await Messages
  .OfTypes<FundsReserved, InventoryLocked>()
  .Take(2)
  .Completion();
```

### Emit a signal to a flow ([source code](https://github.com/stidsborg/Cleipnir.Flows/blob/a4ada3e734634278a81ca8fd25a39e058b628d50/Samples/Cleipnir.Flows.Samples.Console/WaitForMessages/Example.cs#L26)):
```csharp
var messagesWriter = flows.MessagesWriter(orderId);
await messagesWriter.AppendMessage(new FundsReserved(orderId), idempotencyKey: nameof(FundsReserved));
```

### Restart a failed flow ([source code](https://github.com/stidsborg/Cleipnir.Flows/blob/b842b8bdb7367ddd86e8962017c520dadf3a27b2/Samples/Cleipnir.Flows.Samples.Console/RestartFlow/Example.cs#L32)):
```csharp
var controlPanel = await flows.ControlPanel(flowId);
controlPanel!.Param = "valid parameter";
await controlPanel.ReInvoke();
```

### Postpone a running flow (without taking in-memory resources) ([source code](https://github.com/stidsborg/Cleipnir.Flows/blob/b842b8bdb7367ddd86e8962017c520dadf3a27b2/Samples/Cleipnir.Flows.Samples.Console/Postpone/PostponeFlow.cs#L13)):
```csharp
[GenerateFlows]
public class PostponeFlow : Flow<string>
{
  private readonly ExternalService _externalService = new();

  public override async Task Run(string orderId)
  {
    if (await _externalService.IsOverloaded())
      Postpone(delay: TimeSpan.FromMinutes(10));
        
    //execute rest of the flow
  }
}
```

### Add metrics middleware ([source code](https://github.com/stidsborg/Cleipnir.Flows/tree/main/Samples/Cleipnir.Flows.Samples.Console/Middleware)):
```csharp
public class MetricsMiddleware : IMiddleware
{
  private Action IncrementCompletedFlowsCounter { get; }
  private Action IncrementFailedFlowsCounter { get; }
  private Action IncrementRestartedFlowsCounter { get; }

  public MetricsMiddleware(Action incrementCompletedFlowsCounter, Action incrementFailedFlowsCounter, Action incrementRestartedFlowsCounter)
  {
    IncrementCompletedFlowsCounter = incrementCompletedFlowsCounter;
    IncrementFailedFlowsCounter = incrementFailedFlowsCounter;
    IncrementRestartedFlowsCounter = incrementRestartedFlowsCounter;
  }

  public async Task<Result<TResult>> Run<TFlow, TParam, TResult>(
    TParam param, 
    Context context, 
    Next<TFlow, TParam, TResult> next) where TParam : notnull
  {
    var started = workflow.Effect.TryGet<bool>(id: "Started", out _);
    if (started)
      IncrementRestartedFlowsCounter();
    else
      await workflow.Effect.Upsert("Started", true);
        
    var result = await next(param, workflow);
    if (result.Outcome == Outcome.Fail)
      IncrementFailedFlowsCounter();
    else if (result.Outcome == Outcome.Succeed)
      IncrementCompletedFlowsCounter();
        
    return result;
  }
}
```

## Distributed system challenges
When distributed systems needs to cooperator in order to fulfill some business process a system crash or restart may leave the system in an inconsistent state.

Consider the following order-flow:
```csharp
public async Task ProcessOrder(Order order)
{
  await _paymentProviderClient.Reserve(order.TransactionId, order.CustomerId, order.TotalPrice);
  await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
  await _paymentProviderClient.Capture(order.TransactionId);
  await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);
}    
```

Currently, the flow is not resilient against *crashes* or *restarts*.

For instance, if the process crashes just before capturing the funds from the payment provider then the ordered products are shipped to the customer but nothing is deducted from the customer’s credit card.
Not an ideal situation for the business.
No matter how we rearrange the flow a crash might lead to either situation:
- products are shipped to the customer without payment being deducted from the customer’s credit card
- payment is deducted from the customer’s credit card but products are never shipped

**Ensuring flow-restart on crashes or restarts:**

Thus, to rectify the situation we must ensure that the flow is *restarted* if it did not complete in a previous execution.

### RPC-solution
Consider the following Order-flow:

```csharp
[GenerateFlows]
public class OrderFlow : Flow<Order>
{
  private readonly IPaymentProviderClient _paymentProviderClient;
  private readonly IEmailClient _emailClient;
  private readonly ILogisticsClient _logisticsClient;

  public OrderFlow(IPaymentProviderClient paymentProviderClient, IEmailClient emailClient, ILogisticsClient logisticsClient)
  {
    _paymentProviderClient = paymentProviderClient;
    _emailClient = emailClient;
    _logisticsClient = logisticsClient;
  }

  public async Task ProcessOrder(Order order)
  {
    Log.Logger.ForContext<OrderFlow>().Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");

    var transactionId = Guid.Empty;
    await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);
    await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
    await _paymentProviderClient.Capture(transactionId);
    await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

    Log.Logger.ForContext<OrderFlow>().Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' completed");
  }       
}
```
Sometimes simply wrapping a business flow inside the framework is enough.

This would be the case if all the steps in the flow were idempotent. In that situation it is fine to call an endpoint multiple times without causing unintended side-effects.

#### At-least-once & Idempotency

However, in the order-flow presented here this is not the case.

The payment provider requires the caller to provide a transaction-id. Thus, the same transaction-id must be provided when re-executing the flow.

In Cleipnir this challenge is solved by wrapping non-determinism inside effects.

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");

  var transactionId = Effect.Capture("TransactionId", Guid.NewGuid);
  
  await _paymentProviderClient.Reserve(transactionId, order.CustomerId, order.TotalPrice);
  await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
  await _paymentProviderClient.Capture(transactionId);
  await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");
}
```

#### At-most-once API:

For the sake of presenting the framework’s versatility let us assume that the logistics’ API is *not* idempotent and that it is out of our control to change that.

Thus, every time a successful call is made to the logistics service the content of the order is shipped to the customer.

As a result the order-flow must fail if it is restarted and:
* a request was previously sent to logistics-service
* but no response was received.

This can again be accomplished by using effects:

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");

  var transactionId = await Effect.Capture("TransactionId", Guid.NewGuid);
  await _paymentProviderClient.Reserve(order.CustomerId, transactionId, order.TotalPrice);

  await Effect.Capture(
    id: "ShipProducts",
    work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
  );
  
  await _paymentProviderClient.Capture(transactionId);           
  await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");
}  
```

A *failed/exception throwing* flow is not automatically retried by the framework.

Instead, it must be manually restarted by using the flow's associated control-panel. 

**Control Panel:**

Using the flow’s control panel both the parameter and scrapbook may be changed before the flow is retried.

For instance, assuming it is determined that the products where not shipped for a certain order, then the following code re-invokes the order with the state changed accordingly.

```csharp
var controlPanel = await flows.ControlPanel(order.OrderId);
await controlPanel!.Effects.Remove("ShipProducts");
await controlPanel.ReInvoke();
```

### Message-based Solution
Message- or event-driven system are omnipresent in enterprise architectures today.

They fundamentally differ from RPC-based in that:
* messages related to the same order are not delivered to the same process.

This has huge implications in how a flow must be implemented and as a result a simple sequential flow - as in the case of the order-flow:
* becomes fragmented and hard to reason about
* inefficient - each time a message is received the entire state must be reestablished
* inflexible

Cleipnir Flows takes a novel approach by piggy-backing on the features described so far and using event-sourcing and reactive programming together to form a simple and extremely useful abstraction.

As a result the order-flow can be implemented as follows:

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");  

  await _bus.Send(new ReserveFunds(order.OrderId, order.TotalPrice, Scrapbook.TransactionId, order.CustomerId));
  await Messages.NextOfType<FundsReserved>();
            
  await _bus.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
  await Messages.NextOfType<ProductsShipped>();
            
  await _bus.Send(new CaptureFunds(order.OrderId, order.CustomerId, Scrapbook.TransactionId));
  await Messages.NextOfType<FundsCaptured>();

  await _bus.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
  await Messages.NextOfType<OrderConfirmationEmailSent>();

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");      
}
```
There is a bit more going on in the example above compared to the previous RPC-example.
However, the flow is actually very similar to RPC-based. It is sequential and robust. If the flow crashes and is restarted it will continue from the point it got to before the crash.

It is noted that the message broker in the example is just a stand-in - thus not a framework concept - for RabbitMQ, Kafka or some other messaging infrastructure client.

In a real application the message broker would be replaced with the actual way the application broadcasts a message/event to other services.

Furthermore, each flow has an associated private **event source** called **Messages**. When events are received from the network they can be placed into the relevant flow's event source - thereby allowing the flow to continue.

| Did you know? |
| --- |
| The framework allows awaiting events both in-memory or suspending the invocation until an event has been appended to the event source. </br>Thus, allowing the developer to find the sweet-spot per use-case between performance and releasing resources.|
