﻿using System.Diagnostics;
using AdminLte.Mvc.Extensions;
using AdminLte.Mvc.Models;
using AdminLte.Mvc.Models.ChartModels;
using AdminLte.Mvc.Models.Enums;
using AdminLte.Mvc.Models.ViewModels;
using AdminLte.Mvc.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AdminLte.Mvc.Controllers;
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IBTCompanyInfoService _companyInfoService;
    private readonly SignInManager<BTUser> _signInManager;
    private readonly IBTProjectService _projectService;

    public HomeController(ILogger<HomeController> logger, IBTCompanyInfoService companyInfoService, SignInManager<BTUser> signInManager, IBTProjectService projectService)
    {
        _logger = logger;
        _companyInfoService = companyInfoService;
        _signInManager = signInManager;
        _projectService = projectService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        DashboardViewModel model = new();

        int companyId = User.Identity.GetCompanyId().Value;

        model.Company = await _companyInfoService.GetCompanyInfoByIdAsync(companyId);
        model.Projects = (await _companyInfoService.GetAllProjectsAsync(companyId)).Where(p=>p.Archived == false).ToList();
        model.Tickets = model.Projects.SelectMany(p=>p.Tickets).Where(t=>t.Archived == false).ToList();
        model.Members = model.Company.Members.ToList();


        return View(model);
    }

    [HttpPost]
    public async Task<JsonResult> GglProjectTickets()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

        List<object> chartData = new();

        chartData.Add(new object[] { "ProjectName", "TicketCount" });

        foreach (Project prj in projects)
        {
            chartData.Add(new object[] { prj.Name, prj.Tickets.Count() });
        }

        return Json(chartData);
    }

    [HttpPost]
    public async Task<JsonResult> GglProjectPriority()
    {
        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

        List<object> chartData = new();
        chartData.Add(new object[] { "Priority", "Count" });


        foreach (string priority in Enum.GetNames(typeof(BTProjectPriority)))
        {
            int priorityCount = (await _projectService.GetAllProjectsByPriorityAsync(companyId, priority)).Count();
            chartData.Add(new object[] { priority, priorityCount });
        }

        return Json(chartData);
    }


    [HttpPost]
    public async Task<JsonResult> AmCharts()
    {

        AmChartData amChartData = new();
        List<AmItem> amItems = new();

        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = (await _companyInfoService.GetAllProjectsAsync(companyId)).Where(p => p.Archived == false).ToList();

        foreach (Project project in projects)
        {
            AmItem item = new();

            item.Project = project.Name;
            item.Tickets = project.Tickets.Count;
            item.Developers = (await _projectService.GetProjectMembersByRoleAsync(project.Id, nameof(Roles.Developer))).Count;

            amItems.Add(item);
        }

        amChartData.Data = amItems.ToArray();


        return Json(amChartData.Data);
    }

    [HttpPost]
    public async Task<JsonResult> PlotlyBarChart()
    {
        PlotlyBarData plotlyData = new();
        List<PlotlyBar> barData = new();

        int companyId = User.Identity.GetCompanyId().Value;

        List<Project> projects = await _projectService.GetAllProjectsByCompanyAsync(companyId);

        //Bar One
        PlotlyBar barOne = new()
        {
            X = projects.Select(p => p.Name).ToArray(),
            Y = projects.SelectMany(p => p.Tickets).GroupBy(t => t.ProjectId).Select(g => g.Count()).ToArray(),
            Name = "Tickets",
            Type = "bar"
        };

        //Bar Two
        PlotlyBar barTwo = new()
        {
            X = projects.Select(p => p.Name).ToArray(),
            Y = projects.Select(async p => (await _projectService.GetProjectMembersByRoleAsync(p.Id, nameof(Roles.Developer))).Count).Select(c => c.Result).ToArray(),
            Name = "Developers",
            Type = "bar"
        };

        barData.Add(barOne);
        barData.Add(barTwo);

        plotlyData.Data = barData;

        return Json(plotlyData);
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
