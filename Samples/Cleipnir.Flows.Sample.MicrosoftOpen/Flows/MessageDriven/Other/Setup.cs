namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;

public static class Setup
{
    public static void AddInMemoryBus(this IServiceCollection services)
    {
        services.AddSingleton<Bus>(p =>
        {
            var flowsContainer = p.GetRequiredService<FlowsContainer>();
            var bus = new Bus(flowsContainer);
            
            var emailService = new EmailServiceStub(bus);
            var logisticsService = new LogisticsServiceStub(bus);
            var paymentProviderService = new PaymentProviderStub(bus);
            
            emailService.Initialize();
            logisticsService.Initialize();
            paymentProviderService.Initialize();

            return bus;
        });
    }
}