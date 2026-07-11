using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_WARD")]
public partial class TM_WARD : AuditableEntity
{
    public int? INT_CITY_ID { get; set; }
    public string? CHR_WARD_CODE { get; set; }
    public string? NVCHR_WARD_NAME { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
