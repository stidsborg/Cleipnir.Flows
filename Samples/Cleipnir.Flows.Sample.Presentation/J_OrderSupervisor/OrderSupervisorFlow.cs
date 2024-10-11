namespace Cleipnir.Flows.Sample.Presentation.J_OrderSupervisor;

[GenerateFlows]
public class OrderSupervisorFlow : Flow<Order>
{
    public override async Task Run(Order order)
    {
        try
        {
            await Message<FundsReserved>(TimeSpan.FromMinutes(5));
        }
        catch (TimeoutException)
        {
            await AlertBusinessOfStaleOrder(order);
            await Message<FundsReserved>();
        }
        
        //...
    }

    private Task AlertBusinessOfStaleOrder(Order order)
    {
        return Task.CompletedTask;
    }
}
