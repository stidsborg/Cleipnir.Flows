using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;

public class ServiceBusHandler(MessageDrivenOrderFlows flows) : 
    IHandleMessages<FundsReserved>,
    IHandleMessages<FundsReservationFailed>,
    IHandleMessages<FundsCaptured>,
    IHandleMessages<FundsCaptureFailed>,
    IHandleMessages<ProductsShipped>,
    IHandleMessages<ProductsShipmentFailed>,
    IHandleMessages<OrderConfirmationEmailSent>,
    IHandleMessages<OrderConfirmationEmailFailed>
{
    public Task Handle(FundsReserved message) => 
        flows.SendMessage(message.OrderId, message);

    public Task Handle(FundsReservationFailed message)
        => flows.SendMessage(message.OrderId, message);

    public Task Handle(FundsCaptured message)
        => flows.SendMessage(message.OrderId, message);
    
    public Task Handle(FundsCaptureFailed message)
        => flows.SendMessage(message.OrderId, message);

    public Task Handle(ProductsShipped message)
        => flows.SendMessage(message.OrderId, message);

    public Task Handle(ProductsShipmentFailed message)
        => flows.SendMessage(message.OrderId, message);

    public Task Handle(OrderConfirmationEmailSent message) 
        => flows.SendMessage(message.OrderId, message);

    public Task Handle(OrderConfirmationEmailFailed message) 
        => flows.SendMessage(message.OrderId, message);
}

public interface IHandleMessages<in TMessage> 
{
    Task Handle(TMessage message);
}