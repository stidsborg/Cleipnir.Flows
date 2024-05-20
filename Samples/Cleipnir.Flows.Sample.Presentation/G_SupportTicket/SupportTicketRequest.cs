namespace Cleipnir.Flows.Sample.Presentation.G_SupportTicket;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);