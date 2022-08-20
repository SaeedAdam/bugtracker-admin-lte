using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AdminLte.Mvc.Models;

public class Notification
{
    public int Id { get; set; }

    [DisplayName("Ticket")]
    public int TicketId { get; set; }

    [Microsoft.Build.Framework.Required]
    [DisplayName("Title")]
    public string Title { get; set; }


    [Microsoft.Build.Framework.Required]
    [DisplayName("Message")]
    public string Message { get; set; }

    [DataType(DataType.Date)]
    [DisplayName("Date")]
    public DateTimeOffset Created { get; set; }

    [DisplayName("Recipient")]
    public string RecipientId { get; set; }

    [DisplayName("Sender")]
    public string SenderId { get; set; }

    [DisplayName("Has been viewed")]
    public bool Viewed { get; set; }



    //-- Navigation properties --//
    public virtual Ticket Ticket { get; set; }
    public virtual BTUser Recipient { get; set; }
    public virtual BTUser Sender { get; set; }
}
