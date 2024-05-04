namespace Cleipnir.Flows.Sample.Presentation.X_SupportTicket.Solution;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);