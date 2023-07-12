# Cleipnir Flows
**"Simply making fault tolerant code simple"**

Cleipnir Flows is a simple and intuitive workflow-as-code .NET framework - ensuring your code completes despite: failures, restarts, deployments, versioning etc.

It is similar to Azure Durable Functions - but simpler, less restrictive and tailored for ASP.NET services.

It works for both RPC and Message-based communication.

## Getting Started
To get started simply perform the following three steps in an ASP.NET service:

Firstly, install the Cleipnir.Flows nuget package (using either Postgres, SqlServer, MySQL or AzureBlob as persistence layer). I.e.
```powershell
Install-Package Cleipnir.Flows.Postgres
```

Secondly, add the following to the setup in `Program.cs`:
```csharp
builder.Services.AddFlows(connectionString);
```

Finally, implement your flow:
```csharp
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

    public override async Task Run(Order order)
    {
        await _paymentProviderClient.Reserve(order.CustomerId, order.TransactionId, order.TotalPrice);
        await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
        await _paymentProviderClient.Capture(order.TransactionId);
        await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);
    }
}
```

The flow can then be started using the corresponding source generated Flows-type:
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

Congrats, any non-completed Order flows are now automatically restarted by the framework.

## Examples
As an example is worth a thousand lines of documentation - various useful examples are presented in the following section:

1: Avoid executing already completed code again if a flow is restarted:
```csharp
public class AtLeastOnceFlow : Flow<string, AtLeastOnceFlowScrapbook, string>
{
  private readonly PuzzleSolverService _puzzleSolverService = new();

  public override async Task<string> Run(string hashCode)
  {
    var solution = await DoAtLeastOnce(
      workStatus: scrapbook => scrapbook.SolutionStatusAndResult,
      work: () => _puzzleSolverService.SolveCryptographicPuzzle(hashCode)
    );

    return solution;
  }
}

public class AtLeastOnceFlowScrapbook : RScrapbook
{
  public WorkStatusAndResult<string> SolutionStatusAndResult { get; set; }
}
```

2: Ensure flow step is **executed at-most-once**:
```csharp
public class AtMostOnceFlow : Flow<string>
{
    private readonly RocketSender _rocketSender = new();
    
    public override async Task Run(string rocketId)
    {
        await DoAtMostOnce(
            workId: "FireRocket",
            _rocketSender.FireRocket
        );
    }
}
```

3: Wait for 2 external messages before continuing flow:
```csharp
public class WaitForMessagesFlow : Flow<string>
{
  public override async Task Run(string param)
  {
    await EventSource
      .OfTypes<FundsReserved, InventoryLocker>()
      .Take(2)
      .ToList();

    System.Console.WriteLine("Complete order-processing");
  }
}
```
Alternatively, the flow can also be suspended to save resources:
```csharp
await EventSource
  .OfTypes<FundsReserved, InventoryLocked>()
  .Chunk(2)
  .SuspendUntilNext();
```

4: Add event/message to Flow's event-source:
```csharp
var eventSourceWriter = flows.EventSourceWriter(orderId);
await eventSourceWriter.AppendEvent(new FundsReserved(orderId), idempotencyKey: nameof(FundsReserved));
```

5: Restart a failed flow:
```csharp
var controlPanel = await flows.ControlPanel(flowId);
controlPanel!.Param = "valid parameter";
await controlPanel.RunAgain();
```

6: Postpone a running flow (without taking in-memory resources):
```csharp
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

7: Add metrics middleware:
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

  public async Task<Result<TResult>> Run<TFlow, TParam, TScrapbook, TResult>(
    TParam param, 
    TScrapbook scrapbook, 
    Context context, 
    Next<TFlow, TParam, TScrapbook, TResult> next) where TParam : notnull where TScrapbook : RScrapbook, new()
  {
    if (context.InvocationMode == InvocationMode.Retry)
      IncrementRestartedFlowsCounter();
        
    var result = await next(param, scrapbook, context);
    if (result.Outcome == Outcome.Fail)
      IncrementFailedFlowsCounter();
    else if (result.Outcome == Outcome.Succeed)
      IncrementCompletedFlowsCounter();
        
    return result;
  }
}
```

## What is it about?
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
public class OrderFlow : Flow<Order, OrderScrapbook>
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

In Cleipnir this challenge is solved by using a *scrapbook*.

A scrapbook is a user-defined sub-type which holds state useful when/if the flow is restarted. Using it one can ensure that the same transaction id is always used for the same order in the following way:


```csharp 
public class Scrapbook : RScrapbook
{
  public Guid TransactionId { get; set; }
}
```

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");

  if (Scrapbook.TransactionId == Guid.Empty)
  {
    Scrapbook.TransactionId = Guid.NewGuid();
    await Scrapbook.Save();
  }
  
  await _paymentProviderClient.Reserve(Scrapbook.TransactionId, order.CustomerId, order.TotalPrice);
  await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
  await _paymentProviderClient.Capture(Scrapbook.TransactionId);
  await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");
}
```

Essentially, a scrapbook is simply a user-defined poco-class which can be saved on demand.

In the example given, the code may be simplified further, as the scrapbook is also saved by the framework before the flow is executed. I.e.

```csharp
public class Scrapbook : RScrapbook
{
   public Guid TransactionId { get; set; } = Guid.NewGuid();
}
```

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");

  await _paymentProviderClient.Reserve(Scrapbook.TransactionId, order.CustomerId, order.TotalPrice);
  await _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds);
  await _paymentProviderClient.Capture(Scrapbook.TransactionId);
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

This can again be accomplished by using the scrapbook:

```csharp
public class Scrapbook : RScrapbook
{
  public Guid TransactionId { get; set; } = Guid.NewGuid();
  public WorkStatus ProductsShippedStatus { get; set; }
}
```

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");
  
  await _paymentProviderClient.Reserve(order.CustomerId, scrapbook.TransactionId, order.TotalPrice);

  await DoAtMostOnce(
    workStatus: scrapbook => scrapbook.ProductsShipped,
    work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
  );
  
  await _paymentProviderClient.Capture(scrapbook.TransactionId);           
  await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");
}  
```

A *failed/exception throwing* flow is not automatically retried by the framework.

Instead it must be manually restarted by using the flow's associated control-panel. 

**Control Panel:**

Using the flow’s control panel both the parameter and scrapbook may be changed before the flow is retried.

For instance, assuming it is determined that the products where not shipped for a certain order, then the following code re-invokes the order with the scrapbook changed accordingly.

```csharp
private readonly RAction<Order, Scrapbook> _rAction;
private async Task Retry(string orderId)
{
  var controlPanel = await _rAction.ControlPanels.For(orderId);
  controlPanel!.Scrapbook.ProductsShippedStatus = WorkStatus.Completed;
  await controlPanel.ReInvoke();
}
```

**At-most-once convenience syntax:**

The framework has built-in support for the at-most-once (and at-least-once) pattern presented above using the scrapbook as follows:

```csharp
public async Task ProcessOrder(Order order, Scrapbook scrapbook, Context context)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");
  
  await _paymentProviderClient.Reserve(order.CustomerId, scrapbook.TransactionId, order.TotalPrice);
  
  await scrapbook.DoAtMostOnce(
    workStatus: s => s.ProductsShippedStatus,
    work: () => _logisticsClient.ShipProducts(order.CustomerId, order.ProductIds)
  );

  await _paymentProviderClient.Capture(scrapbook.TransactionId);           

  await _emailClient.SendOrderConfirmation(order.CustomerId, order.ProductIds);

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");
}
```

### Message-based Solution
Message- or event-driven system are omnipresent in enterprise architectures today.

They fundamentally differ from RPC-based in that:
* messages related to the same order are not delivered to the same process.

This has huge implications in how a saga-flow is implemented and as a result a simple sequential flow - as in the case of the order-flow:
* becomes fragmented and hard to reason about
* inefficient - each time a message is received the entire state must be reestablished
* inflexible

Cleipnir Flows takes a novel approach by piggy-backing on the features described so far and using event-sourcing and reactive programming together to form a simple and extremely useful abstraction.

As a result the order-flow can be implemented as follows:

```csharp
public async Task ProcessOrder(Order order)
{
  Log.Logger.Information($"ORDER_PROCESSOR: Processing of order '{order.OrderId}' started");  

  await _messageBroker.Send(new ReserveFunds(order.OrderId, order.TotalPrice, Scrapbook.TransactionId, order.CustomerId));
  await EventSource.NextOfType<FundsReserved>();
            
  await _messageBroker.Send(new ShipProducts(order.OrderId, order.CustomerId, order.ProductIds));
  await EventSource.NextOfType<ProductsShipped>();
            
  await _messageBroker.Send(new CaptureFunds(order.OrderId, order.CustomerId, Scrapbook.TransactionId));
  await EventSource.NextOfType<FundsCaptured>();

  await _messageBroker.Send(new SendOrderConfirmationEmail(order.OrderId, order.CustomerId));
  await EventSource.NextOfType<OrderConfirmationEmailSent>();

  Log.Logger.ForContext<OrderProcessor>().Information($"Processing of order '{order.OrderId}' completed");      
}
```
There is a bit more going on in the example above compared to the previous RPC-example.
However, the flow is actually very similar to RPC-based. It is sequential and robust. If the flow crashes and is restarted it will continue from the point it got to before the crash.

It is noted that the message broker in the example is just a stand-in - thus not a framework concept - for RabbitMQ, Kafka or some other messaging infrastructure client.

In a real application the message broker would be replaced with the actual way the application broadcasts a message/event to other services.

Furthermore, each flow has an associated private **event source**. When events are received from the network they can be placed into the relevant flow's event source - thereby allowing the flow to continue.

| Did you know? |
| --- |
| The framework allows awaiting events both in-memory or suspending the invocation until an event has been appended to the event source. </br>Thus, allowing the developer to find the sweet-spot per use-case between performance and releasing resources.|