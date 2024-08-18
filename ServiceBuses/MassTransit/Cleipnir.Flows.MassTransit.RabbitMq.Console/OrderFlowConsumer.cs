using Cleipnir.Flows.MassTransit.RabbitMq.Console.Other;
using MassTransit;

namespace Cleipnir.Flows.MassTransit.RabbitMq.Console;

public class OrderFlowConsumer(OrderFlows orderFlows) : 
   IConsumer<FundsReserved>,
   IConsumer<ProductsShipped>,
   IConsumer<FundsCaptured>,
   IConsumer<OrderConfirmationEmailSent>
{
   public Task Consume(ConsumeContext<FundsReserved> context)
      => orderFlows.SendMessage(context.Message.OrderId, context.Message);

   public Task Consume(ConsumeContext<ProductsShipped> context)
      => orderFlows.SendMessage(context.Message.OrderId, context.Message);

   public Task Consume(ConsumeContext<FundsCaptured> context)
      => orderFlows.SendMessage(context.Message.OrderId, context.Message);

   public Task Consume(ConsumeContext<OrderConfirmationEmailSent> context)
      => orderFlows.SendMessage(context.Message.OrderId, context.Message);
}


public static class Thing
{
   public static void Do()
   {
      System.Console.WriteLine(typeof(OrderFlowConsumer));
      
   }
} 