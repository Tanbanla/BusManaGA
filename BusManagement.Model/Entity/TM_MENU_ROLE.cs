using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_MENU_ROLE")]
public partial class TM_MENU_ROLE : AuditableEntity
{
    public int INT_MENU_ID { get; set; }
    public int? INT_ROLE_ID { get; set; }
}
