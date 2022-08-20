using System.ComponentModel;

namespace AdminLte.Mvc.Models;

public class TicketComment
{
    public int Id { get; set; }

    [DisplayName("Ticket")]
    public int TicketId { get; set; }

    [DisplayName("Member Comment")]
    public string Comment { get; set; }

    [DisplayName("Date")]
    public DateTimeOffset Created { get; set; }

    [DisplayName("Team Member")]
    public string UserId { get; set; }


    //Navigation properties
    public virtual Ticket Ticket { get; set; }
    public virtual BTUser User { get; set; }
}
