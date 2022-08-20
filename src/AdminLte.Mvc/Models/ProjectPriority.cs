using System.ComponentModel;

namespace AdminLte.Mvc.Models;

public class ProjectPriority
{
    public int Id { get; set; }

    [DisplayName("Priority Name")]
    public string Name { get; set; }
}