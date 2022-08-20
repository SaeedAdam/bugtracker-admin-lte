using AdminLte.Mvc.Models;

namespace AdminLte.Mvc.Services.Interfaces;

public interface IBTLookupService
{
    Task<List<TicketPriority>> GetTicketPrioritiesAsync();
    Task<List<TicketStatus>> GetTicketStatusesAsync();
    Task<List<TicketType>> GetTicketTypesAsync();
    Task<List<ProjectPriority>> GetProjectPrioritiesAsync();
}
