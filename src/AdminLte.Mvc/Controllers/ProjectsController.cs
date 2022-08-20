using AdminLte.Mvc.Extensions;
using AdminLte.Mvc.Models;
using AdminLte.Mvc.Models.Enums;
using AdminLte.Mvc.Models.ViewModels;
using AdminLte.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdminLte.Mvc.Controllers;

[Authorize]
public class ProjectsController : Controller
{
    private readonly IBTRolesService _rolesService;
    private readonly IBTLookupService _lookupsService;
    private readonly IBTFileService _fileService;
    private readonly IBTProjectService _projectService;
    private readonly UserManager<BTUser> _userManager;
    private readonly IBTCompanyInfoService _companyInfoService;

    public ProjectsController(IBTRolesService rolesService, IBTLookupService lookupsService, IBTFileService fileService, IBTProjectService projectService, UserManager<BTUser> userManager, IBTCompanyInfoService companyInfoService)
    {
        _rolesService = rolesService;
        _lookupsService = lookupsService;
        _fileService = fileService;
        _projectService = projectService;
        _userManager = userManager;
        _companyInfoService = companyInfoService;
    }

    public async Task<IActionResult> MyProjects()
    {
        string userId = _userManager.GetUserId(User);

        var projects = await _projectService.GetUserProjectsAsync(userId);

        return View(projects);
    }

    public async Task<IActionResult> AllProjects()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = new();

        if (User.IsInRole(nameof(Roles.Admin)) || User.IsInRole(nameof(Roles.ProjectManager)))
        {
            projects = await _companyInfoService.GetAllProjectsAsync(companyId);
        }
        else
        {
            projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);
        }

        return View(projects);
    }
    public async Task<IActionResult> ArchivedProjects()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        var projects = await _projectService.GetArchivedProjectsByCompanyAsync(companyId);

        return View(projects);
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    public async Task<IActionResult> UnassignedProjects()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = await _projectService.GetUnassignedProjectsAsync(companyId);

        return View(projects);
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpGet]
    public async Task<IActionResult> AssignPM(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        AssignPMViewModel model = new();

        model.Project = await _projectService.GetProjectByIdAsync(id, companyId);
        model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(nameof(Roles.ProjectManager), companyId), "Id", "FullName");

        return View(model);
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPM(AssignPMViewModel model)
    {
        if (!string.IsNullOrEmpty(model.PMID))
        {
            await _projectService.AddProjectManagerAsync(model.PMID, model.Project.Id);

            return RedirectToAction(nameof(Details), new {id = model.Project.Id});
        }

        return RedirectToAction(nameof(AssignPM), new {projectId = model.Project.Id});
    }

    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpGet]
    public async Task<IActionResult> AssignMembers(int id)
    {
        ProjectsMemberViewModel model = new();

        int companyId = User.Identity.GetCompanyId().Value;

        model.Project = await _projectService.GetProjectByIdAsync(id, companyId);

        List<BTUser> developers = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Developer), companyId);
        List<BTUser> submitters = await _rolesService.GetUsersInRoleAsync(nameof(Roles.Submitter), companyId);

        List<BTUser> companyMembers = developers.Concat(submitters).ToList();

        List<string> projectsMembers = model.Project.Members.Select(m=>m.Id).ToList();


        model.Users = new MultiSelectList(companyMembers, "Id", "FullName", projectsMembers);

        return View(model);
    }

    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMembers(ProjectsMemberViewModel model)
    {
        if (model.SelectedUsers is not null)
        {
            List<string> membersId = (await _projectService.GetAllProjectMembersExceptPMAsync(model.Project.Id))
                .Select(m => m.Id).ToList();


            // Remove current members
            foreach (string member in membersId)
            {
                await _projectService.RemoveUserFromProjectAsync(member, model.Project.Id);
            }

            // Add new members
            foreach (var member in model.SelectedUsers)
            {
                await _projectService.AddUserToProjectAsync(member, model.Project.Id);
            }

            return RedirectToAction(nameof(Details), new { id = model.Project.Id });
        }

        return RedirectToAction(nameof(AssignMembers), new { projectId = model.Project.Id });
    }
    

    // GET: Projects/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        int companyId = User.Identity.GetCompanyId().Value;


        var project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    // GET: Projects/Create
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> Create()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        AddProjectWithPMViewModel model = new();

        // Load SelectList with data ie. PMList & PriorityList 
        model.PMList =
            new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId),
                "Id", "FullName");

        model.PriorityList = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name");

        return View(model);
    }

    // POST: Projects/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddProjectWithPMViewModel model)
    {
        if (model is not null)
        {
            int companyId = User.Identity.GetCompanyId().Value;

            try
            {
                if (model.Project.ImageFormFile is not null)
                {
                    model.Project.ImageFileData =
                        await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                    model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                    model.Project.ImageFileContentType = model.Project.ImageFormFile.ContentType;
                }

                model.Project.CompanyId = companyId;

                await _projectService.AddNewProjectAsync(model.Project);

                // Add PM if one was chosen
                if (!string.IsNullOrEmpty(model.PMId))
                {
                    await _projectService.AddProjectManagerAsync(model.PMId, model.Project.Id);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //TODO: Redirect to All Projects
            return RedirectToAction("MyProjects");
        }

        return RedirectToAction("Create");
    }

    // GET: Projects/Edit/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> Edit(int? id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        AddProjectWithPMViewModel model = new();

        model.Project = await _projectService.GetProjectByIdAsync(id.Value, companyId);

        // Load SelectList with data ie. PMList & PriorityList 
        model.PMList = new SelectList(await _rolesService.GetUsersInRoleAsync(Roles.ProjectManager.ToString(), companyId),
            "Id", "FullName");
        model.PriorityList = new SelectList(await _lookupsService.GetProjectPrioritiesAsync(), "Id", "Name");

        return View(model);
    }

    // POST: Projects/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AddProjectWithPMViewModel model)
    {
        if (model is not null)
        {
            try
            {
                if (model.Project.ImageFormFile is not null)
                {
                    model.Project.ImageFileData =
                        await _fileService.ConvertFileToByteArrayAsync(model.Project.ImageFormFile);
                    model.Project.ImageFileName = model.Project.ImageFormFile.FileName;
                    model.Project.ImageFileContentType = model.Project.ImageFormFile.ContentType;
                }


                await _projectService.UpdateProjectAsync(model.Project);

                // Add PM if one was chosen
                if (!string.IsNullOrEmpty(model.PMId))
                {
                    await _projectService.AddProjectManagerAsync(model.PMId, model.Project.Id);
                }

                return RedirectToAction("MyProjects");

            }
            catch (DbUpdateConcurrencyException)
            {
                if (! await ProjectExists(model.Project.Id))
                {
                    return NotFound();
                }
  
                Console.WriteLine("Error Editing project.");
                throw;

            }
        }

        return RedirectToAction("Edit");
    }

    // GET: Projects/Archive/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> Archive(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        int companyId = User.Identity.GetCompanyId().Value;

        Project project = null;

        if (User.IsInRole(Roles.Admin.ToString()))
        {
            project = await _projectService.GetProjectByIdForAdminAsync(id.Value);
        }
        else
        {
            project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
        }

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    // POST: Projects/Archive/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost, ActionName("Archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveConfirmed(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        var project = await _projectService.GetProjectByIdAsync(id, companyId);

        await _projectService.ArchiveProjectAsync(project);

        return RedirectToAction(nameof(MyProjects));
    }


    // GET: Projects/Restore/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> Restore(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        int companyId = User.Identity.GetCompanyId().Value;

        Project project = null;

        if (User.IsInRole(Roles.Admin.ToString()))
        {
            project = await _projectService.GetProjectByIdForAdminAsync(id.Value);
        }
        else
        {
            project = await _projectService.GetProjectByIdAsync(id.Value, companyId);
        }

        if (project == null)
        {
            return NotFound();
        }

        return View(project);
    }

    // POST: Projects/Restore/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost, ActionName("Restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreConfirmed(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        var project = await _projectService.GetProjectByIdAsync(id, companyId);

        await _projectService.RestoreProjectAsync(project);

        return RedirectToAction(nameof(MyProjects));
    }

    private async Task<bool> ProjectExists(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        return (await _projectService.GetAllProjectsByCompanyAsync(companyId)).Any(p=>p.Id == id);
    }

}