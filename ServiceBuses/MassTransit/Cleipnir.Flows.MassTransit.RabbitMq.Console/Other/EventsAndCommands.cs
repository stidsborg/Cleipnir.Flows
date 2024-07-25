namespace Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;

public record EventsAndCommands;
public record OrderConfirmationEmailSent(string OrderId, Guid CustomerId) : EventsAndCommands;

public record ReserveFunds(string OrderId, decimal Amount, Guid TransactionId, Guid CustomerId) : EventsAndCommands;
public record FundsReserved(string OrderId) : EventsAndCommands;
public record ShipProducts(string OrderId, Guid CustomerId, IEnumerable<Guid> ProductIds) : EventsAndCommands;
public record ProductsShipped(string OrderId, string TrackAndTraceNumber) : EventsAndCommands;

public record SendOrderConfirmationEmail(string OrderId, Guid CustomerId, string TrackAndTraceNumber) : EventsAndCommands;

public record CaptureFunds(string OrderId, Guid CustomerId, Guid TransactionId) : EventsAndCommands;
public record FundsCaptured(string OrderId) : EventsAndCommands;
public record CancelFundsReservation(string OrderId, Guid TransactionId) : EventsAndCommands;
public record FundsReservationCancelled(string OrderId) : EventsAndCommands;