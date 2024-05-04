namespace Cleipnir.Flows.Sample.Presentation.D_SupportTicket;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);