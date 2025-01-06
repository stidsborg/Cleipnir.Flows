namespace Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven;

public class OrderProcessingException : Exception
{
    public OrderProcessingException(string message) : base(message) { }
}