using AdminLte.Mvc.Models;
using AdminLte.Mvc.Services.Interfaces;

namespace AdminLte.Mvc.Services;

public class BTTicketHistoryService : IBTTicketHistory
{
    private readonly ApplicationDbContext _context;

    public BTTicketHistoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddHistoryAsync(Ticket oldTicket, Ticket newTicket, string userId)
    {
        // New Ticket has been Added 
        if (oldTicket is null && newTicket is not null)
        {
            TicketHistory history = new()
            {
                TicketId = newTicket.Id,
                Property = "",
                OldValue = "",
                NewValue = "",
                Created = DateTimeOffset.Now.ToUniversalTime(),
                UserId = userId,
                Description = "New Ticket Created"
            };

            try
            {
                await _context.TicketHistories.AddAsync(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        else
        {
            // Check Ticket Title
            if (oldTicket.Title != newTicket.Title)
            {
                TicketHistory history = new()
                {
                    TicketId = newTicket.Id,
                    Property = "Title",
                    OldValue = oldTicket.Title,
                    NewValue = newTicket.Title,
                    Created = DateTimeOffset.Now.ToUniversalTime(),
                    UserId = userId,
                    Description = $"New Ticket Title: {newTicket.Title}"
                };

                await _context.TicketHistories.AddAsync(history);
            }

            // Check Ticket Description
            if (oldTicket.Description != newTicket.Description)
            {
                TicketHistory history = new()
                {
                    TicketId = newTicket.Id,
                    Property = "Description",
                    OldValue = oldTicket.Description,
                    NewValue = newTicket.Description,
                    Created = DateTimeOffset.Now.ToUniversalTime(),
                    UserId = userId,
                    Description = $"New Ticket Description: {newTicket.Description}"
                };

                await _context.TicketHistories.AddAsync(history);
            }

            // Check Ticket Priority
            if (oldTicket.TicketPriority != newTicket.TicketPriority)
            {
                TicketHistory history = new()
                {
                    TicketId = newTicket.Id,
                    Property = "TicketPriority",
                    OldValue = oldTicket.TicketPriority.Name,
                    NewValue = newTicket.TicketPriority.Name,
                    Created = DateTimeOffset.Now.ToUniversalTime(),
                    UserId = userId,
                    Description = $"New Ticket Priority: {newTicket.TicketPriority.Name}"
                };

                await _context.TicketHistories.AddAsync(history);
            }

            // Check Ticket Status
            if (oldTicket.TicketStatus != newTicket.TicketStatus)
            {
                TicketHistory history = new()
                {
                    TicketId = newTicket.Id,
                    Property = "TicketStatus",
                    OldValue = oldTicket.TicketStatus.Name,
                    NewValue = newTicket.TicketStatus.Name,
                    Created = DateTimeOffset.Now.ToUniversalTime(),
                    UserId = userId,
                    Description = $"New Ticket Status: {newTicket.TicketStatus.Name}"
                };

                await _context.TicketHistories.AddAsync(history);
            }

            // Check Ticket Type
            if (oldTicket.TicketType != newTicket.TicketType)
            {
                TicketHistory history = new()
                {
                    TicketId = newTicket.Id,
                    Property = "TicketType",
                    OldValue = oldTicket.TicketType.Name,
                    NewValue = newTicket.TicketType.Name,
                    Created = DateTimeOffset.Now.ToUniversalTime(),
                    UserId = userId,
                    Description = $"New Ticket Type: {newTicket.TicketType.Name}"
                };

                await _context.TicketHistories.AddAsync(history);
            }

            // Check Ticket Developer
            if (oldTicket.DeveloperUserId != newTicket.DeveloperUserId)
            {
                TicketHistory history = new()
                {
                    TicketId = newTicket.Id,
                    Property = "Developer",
                    OldValue = oldTicket.DeveloperUser?.FullName ?? "Not Assigned",
                    NewValue = newTicket.DeveloperUser?.FullName,
                    Created = DateTimeOffset.Now.ToUniversalTime(),
                    UserId = userId,
                    Description = $"New Ticket Developer: {newTicket.DeveloperUser.FullName}"
                };

                await _context.TicketHistories.AddAsync(history);
            }

            // Save the TicketHistories DbSet to the database
            await _context.SaveChangesAsync();
        }
    }
    public async Task AddHistoryAsync(int ticketId, string model, string userId)
    {
        try
        {
            Ticket ticket = await _context.Tickets.FindAsync(ticketId);
            string description = model.ToLower().Replace("ticket", "");
            description = $"New {description} added to ticket: {ticket.Title}";

            TicketHistory history = new()
            {
                TicketId = ticket.Id,
                Property = model,
                OldValue = "",
                NewValue = "",
                Created = DateTimeOffset.Now.ToUniversalTime(),
                UserId = userId,
                Description = description
            };

            await _context.TicketHistories.AddAsync(history);
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<List<TicketHistory>> GetProjectTicketsHistoriesAsync(int projectId, int companyId)
    {
        Project project = await _context.Projects.Where(p => p.CompanyId == companyId)
                                                .Include(p => p.Tickets)
                                                .ThenInclude(t => t.History)
                                                .ThenInclude(h => h.User)
                                                .FirstOrDefaultAsync(p => p.Id == projectId);

        List<TicketHistory> ticketHistory = project.Tickets.SelectMany(t => t.History).ToList();

        return ticketHistory;
    }

    public async Task<List<TicketHistory>> GetCompanyTicketsHistoriesAsync(int companyId)
    {
        List<Project> projects = (await _context.Companies
                                                .Include(c => c.Projects)
                                                    .ThenInclude(p => p.Tickets)
                                                        .ThenInclude(t => t.History)
                                                            .ThenInclude(h => h.User)
                                                .FirstOrDefaultAsync(c => c.Id == companyId)).Projects.ToList();

        List<Ticket> tickets = projects.SelectMany(p => p.Tickets).ToList();

        List<TicketHistory> ticketHistories = tickets.SelectMany(t => t.History).ToList();

        return ticketHistories;
    }
}
