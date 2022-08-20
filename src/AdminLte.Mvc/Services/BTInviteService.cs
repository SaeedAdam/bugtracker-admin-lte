using AdminLte.Mvc.Models;
using AdminLte.Mvc.Services.Interfaces;

namespace AdminLte.Mvc.Services;

public class BTInviteService : IBTInviteService
{
    private readonly ApplicationDbContext _context;

    public BTInviteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AcceptInviteAsync(Guid? token, string userId, int companyId)
    {
        Invite invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);

        if (invite is null)
        {
            return false;
        }

        try
        {
            invite.IsValid = false;
            invite.InviteeId = userId;

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task AddNewInviteAsync(Invite invite)
    {
        try
        {
            await _context.Invites.AddAsync(invite);
            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<bool> AnyInviteAsync(Guid token, string email, int companyId)
    {
        try
        {
            return await _context.Invites.Where(i => i.CompanyId == companyId)
                                        .AnyAsync(i => i.CompanyToken == token && i.InviteeEmail == email);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Invite> GetInviteAsync(int inviteId, int companyId)
    {
        try
        {
            Invite invite = await _context.Invites.Where(i => i.CompanyId == companyId)
                    .Include(i => i.Company)
                    .Include(i => i.Project)
                    .Include(i => i.Invitor)
                    .FirstOrDefaultAsync(i => i.Id == inviteId);

            return invite;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Invite> GetInviteAsync(Guid token, string email, int companyId)
    {
        try
        {
            Invite invite = await _context.Invites.Where(i => i.CompanyId == companyId)
                .Include(i => i.Company)
                .Include(i => i.Project)
                .Include(i => i.Invitor)
                .FirstOrDefaultAsync(i => i.CompanyToken == token && i.InviteeEmail == email);

            return invite;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<bool> ValidateInviteCodeAsync(Guid? token)
    {
        if (token is null)
        {
            return false;
        }

        bool result = false;

        try
        {
            Invite invite = await _context.Invites.FirstOrDefaultAsync(i => i.CompanyToken == token);

            if (invite is not null)
            {
                //Determine invite date
                DateTimeOffset inviteDate = invite.InviteDate.DateTime.ToUniversalTime();

                // Custom validation of invite based on the date it was issued 
                // In this we are allowing an invite to be valid for 7 days
                bool validDate = (DateTime.UtcNow - inviteDate).TotalDays <= 7;

                if (validDate)
                {
                    result = invite.IsValid;
                }
            }

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
