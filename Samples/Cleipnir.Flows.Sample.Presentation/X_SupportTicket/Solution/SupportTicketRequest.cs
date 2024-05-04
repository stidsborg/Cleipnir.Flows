namespace Cleipnir.Flows.Sample.Presentation.D_SupportTicket.Solution;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);