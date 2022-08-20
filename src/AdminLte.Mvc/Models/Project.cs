using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AdminLte.Mvc.Models;

public class Project
{
    public int Id { get; set; }

    [DisplayName("Company")]
    public int? CompanyId { get; set; }

    [Required]
    [StringLength(50)]
    [DisplayName("Project Name")]
    public string Name { get; set; }

    [DisplayName("Description")]
    public string Description { get; set; }

    [DataType(DataType.Date)]
    [DisplayName("Start Date")]
    public DateTimeOffset StartDate { get; set; }

    [DataType(DataType.Date)]
    [DisplayName("End Date")]
    public DateTimeOffset EndDate { get; set; }

    [DisplayName("Project Priority")]
    public int? ProjectPriorityId { get; set; }

    [NotMapped]
    [DataType(DataType.Upload)]
    public IFormFile ImageFormFile { get; set; }

    [DisplayName("Image Name")]
    public string ImageFileName { get; set; }
    public byte[] ImageFileData { get; set; }

    [DisplayName("File Extension")]
    public string ImageFileContentType { get; set; }

    [DisplayName("Archived")]
    public bool Archived { get; set; }



    //-- Navigation properties --//
    public virtual Company Company { get; set; }
    public virtual ProjectPriority ProjectPriority { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();
    public virtual ICollection<BTUser> Members { get; set; } = new HashSet<BTUser>();
}