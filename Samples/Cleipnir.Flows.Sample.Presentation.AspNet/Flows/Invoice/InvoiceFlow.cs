using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Invoice;

[GenerateFlows]
public class InvoiceFlow(ILogger<InvoiceFlow> logger) : Flow<CustomerNumber>
{
    public override async Task Run(CustomerNumber customerNumber)
    {
        logger.LogInformation($"CUSTOMER_{customerNumber}: (Re)started flow");
        var invoiceDate = await Effect.Capture(
            () => DateTime.UtcNow.ToFirstOfMonth().AddMonths(1) //.AddSeconds(5)
        );
        
        while (true)
        {
            await Delay(until: invoiceDate); //if past then just complete...
            await Capture(() => SendInvoice(customerNumber, invoiceDate));
            invoiceDate = invoiceDate.AddMonths(1); //.AddSeconds(5)
        }

        /* taming state-explosion
        await Loop.Iterate(
            initial: DateTime.Today,
            next: prev => prev.AddMonths(1),
            work: date => SendInvoice(customerNumber, date)
        );*/
    }
    
    private async Task SendInvoice(CustomerNumber customerNumber, DateTime invoiceDate)
    {
        logger.LogInformation($"CUSTOMER_{customerNumber}: Sending invoice '{invoiceDate:s}'");
        var outstandingAmount = await CalculateInvoiceAmount(customerNumber);
        await EmailInvoice(customerNumber, invoiceDate, outstandingAmount);
    }

    private Task<decimal> CalculateInvoiceAmount(CustomerNumber customerNumber) => 1.2M.ToTask();
    private Task EmailInvoice(CustomerNumber customerNumber, DateTime invoiceDate, decimal invoiceAmount) => Task.CompletedTask;
}

internal static class DateTimeExtensions 
{
    public static DateTime ToFirstOfMonth(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, day: 1, hour: 0, minute: 0, second: 0, kind: date.Kind);
    }
}