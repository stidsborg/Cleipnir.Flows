using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Invoice;
using Microsoft.AspNetCore.Mvc;

namespace Cleipnir.Flows.Sample.MicrosoftOpen.Controllers;

[ApiController]
[Route("[controller]")]
public class CustomerController(InvoiceFlows invoiceFlows) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Post(int customerNumber)
    {
        await invoiceFlows.Schedule(customerNumber.ToString(), new CustomerNumber(customerNumber));
        return Ok();
    }
    
    [HttpGet]
    public async Task<ActionResult> Get(int customerNumber)
    {
        var controlPanel = await invoiceFlows.ControlPanel(customerNumber.ToString());
        if (controlPanel is null)
            return NotFound();

        var str = await controlPanel.ToPrettyString();
        return Ok(str);
    }
}