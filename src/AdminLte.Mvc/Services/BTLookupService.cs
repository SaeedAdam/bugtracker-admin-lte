using AdminLte.Mvc.Models;
using AdminLte.Mvc.Services.Interfaces;

namespace AdminLte.Mvc.Services;

public class BTLookupService : IBTLookupService
{
    private readonly ApplicationDbContext _context;

    public BTLookupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TicketPriority>> GetTicketPrioritiesAsync()
    {
        try
        {
            return await _context.TicketPriorities.ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<TicketStatus>> GetTicketStatusesAsync()
    {
        try
        {
            return await _context.TicketStatuses.ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<TicketType>> GetTicketTypesAsync()
    {
        try
        {
            return await _context.TicketTypes.ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<ProjectPriority>> GetProjectPrioritiesAsync()
    {
        try
        {
            return await _context.ProjectPriorities.ToListAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
