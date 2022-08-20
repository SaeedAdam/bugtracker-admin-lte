using AdminLte.Mvc.Models;

namespace AdminLte.Mvc.Services.Interfaces;

public interface IBTTicketHistory
{
    Task AddHistoryAsync(Ticket oldTicket, Ticket newTicket, string userId);
    Task AddHistoryAsync(int ticketId, string model, string userId);
    Task<List<TicketHistory>> GetProjectTicketsHistoriesAsync(int projectId, int companyId);
    Task<List<TicketHistory>> GetCompanyTicketsHistoriesAsync(int companyId);
}
