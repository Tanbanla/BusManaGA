using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_DRIVER")]
public partial class TM_DRIVER : AuditableEntity
{
    public string? NVCHR_DRIVER_NAME { get; set; }
    public string? CHR_DRIVER_PHONE { get; set; }
    public string? CHR_LICENSE_CATEGORY { get; set; }
    public string? CHR_DATE_OF_BIRTH { get; set; }
    public string? CHR_ACCESS_CARD_CODE { get; set; }
    public int? INT_VENDOR_ID { get; set; }
    public string? CHR_AVARTAR_PATH { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
