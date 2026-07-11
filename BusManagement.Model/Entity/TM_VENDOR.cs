using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_VENDOR")]
public partial class TM_VENDOR : AuditableEntity
{
    public string? CHR_VENDOR_CODE { get; set; }
    public string? NVCHR_VENDOR_NAME { get; set; }
    public string? CHR_VENDOR_PHONE { get; set; }
    public string? NVCHR_ADDRESS { get; set; }
    public string? CHR_TAX_IDENTIFY_NUMBER { get; set; }
    public string? NVCHR_DIRECTOR_NAME { get; set; }
    public string? CHR_DIRECTOR_PHONE { get; set; }
    public string? NVCHR_PIC_NAME { get; set; }
    public string? CHR_PIC_PHONE { get; set; }
}
