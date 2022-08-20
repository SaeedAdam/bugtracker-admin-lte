using AdminLte.Mvc.Models;
using AdminLte.Mvc.Models.Enums;
using AdminLte.Mvc.Services.Interfaces;

namespace AdminLte.Mvc.Services;

public class BTProjectService : IBTProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IBTRolesService _rolesService;

    public BTProjectService(ApplicationDbContext context, IBTRolesService rolesService)
    {
        _context = context;
        _rolesService = rolesService;
    }

    public async Task AddNewProjectAsync(Project project)
    {
        _context.Add(project);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> AddProjectManagerAsync(string userId, int projectId)
    {
        BTUser currentPM = await GetProjectManagerAsync(projectId);

        if (currentPM is not null)
        {
            try
            {
                await RemoveProjectManagerAsync(projectId);
            }
            catch (Exception e)
            {
                Console.WriteLine($"*** Error *** - Error removing current PM from project.  --> Error: {e.Message}");
                return false;
            }
        }

        // Add the new PM
        try
        {
            await AddUserToProjectAsync(userId, projectId);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"*** Error *** - Error Adding new PM.  ---> {e.Message}");
            return false;
        }

    }

    public async Task<bool> AddUserToProjectAsync(string userId, int projectId)
    {
        BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null)
        {
            Project project = await _context.Projects.FirstOrDefaultAsync(u => u.Id == projectId);

            if (!await IsUserOnProjectAsync(userId, projectId))
            {
                project.Members.Add(user);
                await _context.SaveChangesAsync();

                return true;
            }

            return false;
        }

        return false;
    }

    public async Task ArchiveProjectAsync(Project project)
    {
        project.Archived = true;
        await UpdateProjectAsync(project);

        //Archive the tickets for the project
        foreach (Ticket ticket in project.Tickets)
        {
            ticket.ArchivedByProject = true;
            _context.Update(ticket);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RestoreProjectAsync(Project project)
    {
        project.Archived = false;
        await UpdateProjectAsync(project);

        //Restore the tickets for the project
        foreach (Ticket ticket in project.Tickets)
        {
            ticket.ArchivedByProject = false;
            _context.Update(ticket);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Project>> GetUnassignedProjectsAsync(int companyId)
    {
        List<Project> result = new();

        List<Project> projects = await _context.Projects
            .Include(p => p.ProjectPriority)
            .Where(p => p.CompanyId == companyId)
            .ToListAsync();

        foreach (Project project in projects)
        {
            if ((await GetProjectMembersByRoleAsync(project.Id, nameof(Roles.ProjectManager))).Count == 0)
            {
                result.Add(project);
            }
        }

        return result;
    }

    public async Task<List<Project>> GetAllProjectsByCompanyAsync(int companyId)
    {
        List<Project> projects = new();

        projects = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == false)
            .Include(p => p.Members)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.Comments)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.History)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.Notifications)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.DeveloperUser)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.Attachments)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.TicketStatus)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.TicketPriority)
            .Include(p => p.Tickets)
                .ThenInclude(t => t.TicketType)
            .Include(p => p.ProjectPriority)
            .ToListAsync();

        return projects;
    }

    public async Task<List<Project>> GetAllProjectsByPriorityAsync(int companyId, string priorityName)
    {
        List<Project> projects = await GetAllProjectsByCompanyAsync(companyId);
        int priorityId = await LookupProjectPriorityIdAsync(priorityName);

        return projects.Where(p => p.ProjectPriorityId == priorityId).ToList();
    }

    public async Task<List<BTUser>> GetAllProjectMembersExceptPMAsync(int projectId)
    {
        List<BTUser> developers = await GetProjectMembersByRoleAsync(projectId, Roles.Developer.ToString());
        List<BTUser> submitters = await GetProjectMembersByRoleAsync(projectId, Roles.Submitter.ToString());
        List<BTUser> admins = await GetProjectMembersByRoleAsync(projectId, Roles.Admin.ToString());

        List<BTUser> teamMembers = developers.Concat(submitters).Concat(admins).ToList();

        return teamMembers;
    }

    public async Task<List<Project>> GetArchivedProjectsByCompanyAsync(int companyId)
    {
        List<Project> projects = await _context.Projects.Where(p => p.CompanyId == companyId && p.Archived == true)
                                                        .Include(p => p.Members)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.Comments)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.History)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.Notifications)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.DeveloperUser)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.Attachments)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.TicketStatus)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.TicketPriority)
                                                        .Include(p => p.Tickets)
                                                        .ThenInclude(t => t.TicketType)
                                                        .Include(p => p.ProjectPriority)
                                                        .ToListAsync();

        return projects;
    }

    public async Task<List<BTUser>> GetDevelopersOnProjectAsync(int projectId)
    {
        throw new NotImplementedException();
    }

    public async Task<BTUser> GetProjectManagerAsync(int projectId)
    {
        Project project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        foreach (BTUser member in project?.Members)
        {
            if (await _rolesService.IsUserInRoleAsync(member, Roles.ProjectManager.ToString()))
            {
                return member;
            }
        }

        return null;
    }

    public async Task<List<BTUser>> GetProjectMembersByRoleAsync(int projectId, string role)
    {
        Project project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        List<BTUser> members = new();

        foreach (BTUser user in project.Members)
        {
            if (await _rolesService.IsUserInRoleAsync(user, role))
            {
                members.Add(user);
            }
        }

        return members;
    }

    public async Task<Project> GetProjectByIdAsync(int projectId, int companyId)
    {
        Project project = await _context.Projects
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.TicketPriority)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.TicketStatus)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.TicketType)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.DeveloperUser)
                                        .Include(p => p.Tickets)
                                            .ThenInclude(t => t.OwnerUser)
                                        .Include(p => p.Members)
                                        .Include(p => p.ProjectPriority)
                                        .FirstOrDefaultAsync(p => p.Id == projectId && p.CompanyId == companyId);

        return project;
    }

    public async Task<Project> GetProjectByIdForAdminAsync(int projectId)
    {
        Project project = await _context.Projects
            .Include(p => p.Tickets)
            .Include(p => p.Members)
            .Include(p => p.ProjectPriority)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        return project;
    }

    public async Task<List<BTUser>> GetSubmittersOnProjectAsync(int projectId)
    {
        throw new NotImplementedException();
    }

    public async Task<List<BTUser>> GetUsersNotOnProjectAsync(int projectId, int companyId)
    {
        List<BTUser> users = await _context.Users.Where(u => u.Projects.All(p => p.Id != projectId)).ToListAsync();

        return users.Where(u => u.CompanyId == companyId).ToList();
    }

    public async Task<List<Project>> GetUserProjectsAsync(string userId)
    {
        try
        {
            List<Project> userProjects = (await _context.Users
                .Include(u => u.Projects)
                .ThenInclude(p => p.Company)
                .Include(u => u.Projects)
                .ThenInclude(p => p.Members)
                .Include(u => u.Projects)
                .ThenInclude(p => p.Tickets)
                .Include(u => u.Projects)
                .ThenInclude(t => t.Tickets)
                .ThenInclude(t => t.DeveloperUser)
                .Include(u => u.Projects)
                .ThenInclude(t => t.Tickets)
                .ThenInclude(t => t.OwnerUser)
                .Include(u => u.Projects)
                .ThenInclude(t => t.Tickets)
                .ThenInclude(t => t.TicketPriority)
                .Include(u => u.Projects)
                .ThenInclude(t => t.Tickets)
                .ThenInclude(t => t.TicketStatus)
                .Include(u => u.Projects)
                .ThenInclude(t => t.Tickets)
                .ThenInclude(t => t.TicketType)
                .FirstOrDefaultAsync(u => u.Id == userId)).Projects.ToList();

            return userProjects;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"*** Error *** - Error getting user projects list.  ---> {ex.Message}");
            throw;
        }
    }

    public async Task<bool> IsUserOnProjectAsync(string userId, int projectId)
    {
        Project project = await _context.Projects
            .Include(p => p.Members).
            FirstOrDefaultAsync(p => p.Id == projectId);

        bool result = false;

        if (project != null)
        {
            result = project.Members.Any(m => m.Id == userId);
        }

        return result;
    }

    public async Task<bool> IsAssignedProjectManagerAsync(string userId, int projectId)
    {
        try
        {
            string projectManagerId = (await GetProjectManagerAsync(projectId))?.Id;

            if (projectManagerId == userId)
            {
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<int> LookupProjectPriorityIdAsync(string priorityName)
    {
        int priorityId = (await _context.ProjectPriorities.FirstOrDefaultAsync(p => p.Name == priorityName)).Id;

        return priorityId;
    }

    public async Task RemoveProjectManagerAsync(int projectId)
    {
        try
        {
            Project project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            foreach (BTUser member in project?.Members)
            {
                if (await _rolesService.IsUserInRoleAsync(member, Roles.ProjectManager.ToString()))
                {
                    await RemoveUserFromProjectAsync(member.Id, projectId);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"*** Error *** - Error Removing project manager from project.  ---> {e.Message}");
        }
    }

    public async Task RemoveUsersFromProjectByRoleAsync(string role, int projectId)
    {
        try
        {
            List<BTUser> members = await GetProjectMembersByRoleAsync(projectId, role);
            Project project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            foreach (BTUser user in project.Members)
            {
                project.Members.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"*** Error *** - Error Removing users from project.  ---> {e.Message}");
            throw;
        }
    }

    public async Task RemoveUserFromProjectAsync(string userId, int projectId)
    {
        try
        {
            BTUser user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            Project project = await _context.Projects.FirstOrDefaultAsync(u => u.Id == projectId);

            if (await IsUserOnProjectAsync(userId, projectId))
            {
                project.Members.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"*** Error *** - Error Removing User from project.  ---> {ex.Message}");
            throw;
        }
    }

    public async Task UpdateProjectAsync(Project project)
    {
        _context.Update(project);
        await _context.SaveChangesAsync();
    }
}
