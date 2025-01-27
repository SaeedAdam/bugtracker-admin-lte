﻿using System.IO;
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
public class TicketsController : Controller
{
    private readonly UserManager<BTUser> _userManager;
    private readonly IBTProjectService _projectService;
    private readonly IBTLookupService _lookupService;
    private readonly IBTTicketService _ticketService;
    private readonly IBTFileService _fileService;
    private readonly IBTTicketHistory _historyService;

    public TicketsController(UserManager<BTUser> userManager, IBTProjectService btProjectService, IBTLookupService lookupService, IBTTicketService ticketService, IBTFileService fileService, IBTTicketHistory historyService)
    {
        _userManager = userManager;
        _projectService = btProjectService;
        _lookupService = lookupService;
        _ticketService = ticketService;
        _fileService = fileService;
        _historyService = historyService;
    }

    public async Task<IActionResult> MyTickets()
    {
        BTUser btUser = await _userManager.GetUserAsync(User);

        var tickets = await _ticketService.GetTicketsByUserIdAsync(btUser.Id, btUser.CompanyId);

        return View(tickets);
    }

    public async Task<IActionResult> AllTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Ticket> tickets = await _ticketService.GetAllTicketsByCompanyAsync(companyId);

        if (User.IsInRole(nameof(Roles.Developer)) || User.IsInRole(nameof(Roles.Submitter)))
        {
            return View(tickets.Where(t => t.Archived == false));
        }

        return View(tickets);
    }

    public async Task<IActionResult> ArchivedTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Ticket> tickets = await _ticketService.GetArchivedTicketsAsync(companyId);

        return View(tickets);
    }

    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> UnassignedTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        string btUserId = _userManager.GetUserId(User);

        List<Ticket> tickets = await _ticketService.GetUnassignedTicketsAsync(companyId);

        if (User.IsInRole(nameof(Roles.Admin)))
        {
            return View(tickets);
        }
        else
        {
            List<Ticket> pmTickets = new();

            foreach (Ticket ticket in tickets)
            {
                if (await _projectService.IsAssignedProjectManagerAsync(btUserId, ticket.ProjectId))
                {
                    pmTickets.Add(ticket);
                }
            }

            return View(pmTickets);
        }
    }

    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpGet]
    public async Task<IActionResult> AssignDeveloper(int id)
    {
        AssignDeveloperViewModel model = new();

        model.Ticket = await _ticketService.GetTicketByIdAsync(id);
        model.Developers =
            new SelectList(
                await _projectService.GetProjectMembersByRoleAsync(model.Ticket.ProjectId, nameof(Roles.Developer)), "Id", "FullName");

        return View(model);

    }

    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignDeveloper(AssignDeveloperViewModel model)
    {
        if (model.DeveloperId is not null)
        {
            BTUser btUser = await _userManager.GetUserAsync(User);

            Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);

            try
            {
                await _ticketService.AssignTicketAsync(model.Ticket.Id, model.DeveloperId);


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(model.Ticket.Id);
            await _historyService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);

            return RedirectToAction(nameof(Details), new { id = model.Ticket.Id });
        }
        
        return RedirectToAction(nameof(AssignDeveloper), new {id = model.Ticket.Id});
    }

    // GET: Tickets/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    // GET: Tickets/Create
    public async Task<IActionResult> Create()
    {
        BTUser btUser = await _userManager.GetUserAsync(User);

        int companyId = User.Identity.GetCompanyId().Value;

        if (User.IsInRole(nameof(Roles.Admin)))
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(companyId), "Id", "Name");
        }
        else
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
        }



        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");

        return View();
    }

    // POST: Tickets/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
    {
        BTUser btUser = await _userManager.GetUserAsync(User);

        if (ModelState.IsValid)
        {
            try
            {
                ticket.Created = DateTimeOffset.Now.ToUniversalTime();
                ticket.OwnerUserId = btUser.Id;
                ticket.TicketStatusId = (await _ticketService.LookupTicketStatusIdAsync(nameof(BTTicketStatus.New))).Value;

                await _ticketService.AddNewTicketAsync(ticket);

                //TODO: Ticket History 
                Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);
                await _historyService.AddHistoryAsync(null, newTicket, btUser.Id);



                //TODO: Ticket Notification

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            return RedirectToAction(nameof(MyTickets));
        }


        if (User.IsInRole(nameof(Roles.Admin)))
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetAllProjectsByCompanyAsync(btUser.CompanyId), "Id", "Name");
        }
        else
        {
            ViewData["ProjectId"] = new SelectList(await _projectService.GetUserProjectsAsync(btUser.Id), "Id", "Name");
        }



        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name");
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name");
        return View(ticket);
    }

    // GET: Tickets/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
        ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

        return View(ticket);
    }

    // POST: Tickets/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,Created,Updated,Archived,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,DeveloperUserId")] Ticket ticket)
    {
        if (id != ticket.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            BTUser btUser = await _userManager.GetUserAsync(User);
            Ticket oldTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

            try
            {
                ticket.Updated = DateTimeOffset.Now.ToUniversalTime();
                await _ticketService.UpdateTicketAsync(ticket);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await TicketExists(ticket.Id))
                {
                    return NotFound();
                }

                throw;
            }

            Ticket newTicket = await _ticketService.GetTicketAsNoTrackingAsync(ticket.Id);

            await _historyService.AddHistoryAsync(oldTicket, newTicket, btUser.Id);


            //TODO: Ticket Notification

            return RedirectToAction(nameof(MyTickets));
        }

        ViewData["TicketPriorityId"] = new SelectList(await _lookupService.GetTicketPrioritiesAsync(), "Id", "Name", ticket.TicketPriorityId);
        ViewData["TicketStatusId"] = new SelectList(await _lookupService.GetTicketStatusesAsync(), "Id", "Name", ticket.TicketStatusId);
        ViewData["TicketTypeId"] = new SelectList(await _lookupService.GetTicketTypesAsync(), "Id", "Name", ticket.TicketTypeId);

        return View(ticket);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTicketComment([Bind("Id,TicketId,Comment")] TicketComment ticketComment)
    {
        if (ModelState.IsValid)
        {
            try
            {
                ticketComment.UserId = _userManager.GetUserId(User);
                ticketComment.Created = DateTimeOffset.Now.ToUniversalTime();

                await _ticketService.AddTicketCommentAsync(ticketComment);


                //Add history 
                await _historyService.AddHistoryAsync(ticketComment.Id, nameof(TicketComment), ticketComment.UserId);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return RedirectToAction("Details", new { id = ticketComment.TicketId });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTicketAttachment([Bind("Id,FormFile,Description,TicketId")] TicketAttachment ticketAttachment)
    {
        string statusMessage;

        if (ModelState.IsValid && ticketAttachment.FormFile is not null)
        {
            try
            {
                ticketAttachment.FileData = await _fileService.ConvertFileToByteArrayAsync(ticketAttachment.FormFile);
                ticketAttachment.FileName = ticketAttachment.FormFile.FileName;
                ticketAttachment.FileContentType = ticketAttachment.FormFile.ContentType;

                ticketAttachment.Created = DateTimeOffset.Now;
                ticketAttachment.UserId = _userManager.GetUserId(User);

                await _ticketService.AddTicketAttachmentAsync(ticketAttachment);
                statusMessage = "Success: New attachment added to Ticket.";

                //Add history
                await _historyService.AddHistoryAsync(ticketAttachment.Id, nameof(TicketAttachment), ticketAttachment.UserId);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
        else
        {
            statusMessage = "Error: Invalid data.";

        }

        return RedirectToAction("Details", new { id = ticketAttachment.TicketId, message = statusMessage });
    }


    public async Task<IActionResult> ShowFile(int id)
    {
        TicketAttachment ticketAttachment = await _ticketService.GetTicketAttachmentByIdAsync(id);
        string fileName = ticketAttachment.FileName;
        byte[] fileData = ticketAttachment.FileData;
        string ext = Path.GetExtension(fileName).Replace(".", "");

        Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}");
        return File(fileData, $"application/{ext}");
    }


    // GET: Tickets/Archive/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> Archive(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    // POST: Tickets/Archive/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost, ActionName("Archive")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveConfirmed(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);

        await _ticketService.ArchiveTicketAsync(ticket);

        return RedirectToAction(nameof(MyTickets));
    }

    // GET: Tickets/Restore/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    public async Task<IActionResult> Restore(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Ticket ticket = await _ticketService.GetTicketByIdAsync(id.Value);

        if (ticket == null)
        {
            return NotFound();
        }

        return View(ticket);
    }

    // POST: Tickets/Restore/5
    [Authorize(Roles = $"{nameof(Roles.Admin)},{nameof(Roles.ProjectManager)}")]
    [HttpPost, ActionName("Restore")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreConfirmed(int id)
    {
        var ticket = await _ticketService.GetTicketByIdAsync(id);

        await _ticketService.RestoreTicketAsync(ticket);

        return RedirectToAction(nameof(MyTickets));
    }

    private async Task<bool> TicketExists(int id)
    {
        int companyId = User.Identity.GetCompanyId().Value;

        return (await _ticketService.GetAllTicketsByCompanyAsync(companyId)).Any(t => t.Id == id);
    }
}