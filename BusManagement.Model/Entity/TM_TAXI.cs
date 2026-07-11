using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_TAXI")]
public partial class TM_TAXI : AuditableEntity
{
    public string? CHR_LICENSE_PLATE { get; set; }
    public string? NVCHR_CATEGORY { get; set; }
    public string? NVCHR_BRAND { get; set; }
    public int? INT_CAPACITY { get; set; }
    public int? INT_DRIVER { get; set; }
    public string? NVCHR_DRIVER { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
