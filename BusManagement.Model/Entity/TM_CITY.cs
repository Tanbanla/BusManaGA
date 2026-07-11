using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_CITY")]
public partial class TM_CITY : AuditableEntity
{
    public string? CHR_CITY_CODE { get; set; }
    public string? NVCHR_CITY_NAME { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
