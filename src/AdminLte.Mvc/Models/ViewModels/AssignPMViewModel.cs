using Microsoft.AspNetCore.Mvc.Rendering;

namespace AdminLte.Mvc.Models.ViewModels;

public class AssignPMViewModel
{
    public Project Project { get; set; }
    public SelectList PMList { get; set; }
    public string PMID { get; set; }
}
