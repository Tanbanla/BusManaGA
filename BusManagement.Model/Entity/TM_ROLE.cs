using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_ROLE")]
public partial class TM_ROLE : AuditableEntity
{
    public string? NVCHR_ROLE_NAME { get; set; }
    public string? NVCHR_DESCRIPTION { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
