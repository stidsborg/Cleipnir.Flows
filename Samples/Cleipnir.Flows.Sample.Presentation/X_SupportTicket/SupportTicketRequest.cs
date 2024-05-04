namespace Cleipnir.Flows.Sample.Presentation.X_SupportTicket;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);