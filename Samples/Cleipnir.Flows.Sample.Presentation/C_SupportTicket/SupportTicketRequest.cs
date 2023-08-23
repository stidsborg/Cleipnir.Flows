namespace Cleipnir.Flows.Sample.Presentation.C_SupportTicket;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);