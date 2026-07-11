using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS")]
public partial class TM_BUS : AuditableEntity
{
    public string? CHR_BUS_CODE { get; set; }
    public string? NVCHR_BUS_NAME { get; set; }
    public string? CHR_LICENSE_PLATE { get; set; }
    public string? NVCHR_CATEGORY { get; set; }
    public string? NVCHR_BRAND { get; set; }
    public string? NVCHR_MODEL { get; set; }
    public string? NVCHR_MODEL_YEAR { get; set; }
    public int? INT_CAPACITY { get; set; }
    public int? INT_VENDOR_ID { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
