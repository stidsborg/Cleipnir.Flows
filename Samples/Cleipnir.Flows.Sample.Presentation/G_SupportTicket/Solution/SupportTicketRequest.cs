namespace Cleipnir.Flows.Sample.Presentation.G_SupportTicket.Solution;

public record SupportTicketRequest(Guid SupportTicketId, string[] CustomerSupportAgents);