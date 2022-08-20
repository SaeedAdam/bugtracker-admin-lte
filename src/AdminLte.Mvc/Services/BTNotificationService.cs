using AdminLte.Mvc.Models;
using AdminLte.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace AdminLte.Mvc.Services;

public class BTNotificationService : IBTNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IBTRolesService _rolesService;

    public BTNotificationService(ApplicationDbContext context, IEmailSender emailSender, IBTRolesService rolesService)
    {
        _context = context;
        _emailSender = emailSender;
        _rolesService = rolesService;
    }

    public async Task AddNotificationAsync(Notification notification)
    {
        await _context.AddAsync(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetReceivedNotificationsAsync(string userId)
    {
        List<Notification> receivedNotifications = await _context.Notifications
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .Include(n => n.Ticket)
                .ThenInclude(t => t.Project)
            .Where(n => n.RecipientId == userId).ToListAsync();

        return receivedNotifications;

    }

    public async Task<List<Notification>> GetSentNotificationsAsync(string userId)
    {
        List<Notification> sentNotifications = await _context.Notifications
            .Include(n => n.Recipient)
            .Include(n => n.Sender)
            .Include(n => n.Ticket)
            .ThenInclude(t => t.Project)
            .Where(n => n.SenderId == userId).ToListAsync();

        return sentNotifications;
    }

    public async Task SendEmailNotificationsByRoleAsync(Notification notification, int companyId, string role)
    {
        try
        {
            List<BTUser> members = await _rolesService.GetUsersInRoleAsync(role, companyId);

            foreach (BTUser btUser in members)
            {
                notification.RecipientId = btUser.Id;
                await SendEmailNotificationAsync(notification, notification.Title);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task SendMembersEmailNotificationsAsync(Notification notification, List<BTUser> members)
    {
        try
        {
            foreach (BTUser btUser in members)
            {
                notification.RecipientId = btUser.Id;
                await SendEmailNotificationAsync(notification, notification.Title);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<bool> SendEmailNotificationAsync(Notification notification, string emailSubject)
    {
        BTUser btUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == notification.RecipientId);

        if (btUser is not null)
        {
            string btUserEmail = btUser.Email;
            string message = notification.Message;

            //Send Email 
            try
            {
                await _emailSender.SendEmailAsync(btUserEmail, emailSubject, message);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return false;
    }
}
