using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_DRIVER_VIOLATION")]
public partial class TM_DRIVER_VIOLATION : AuditableEntity
{
    public int? INT_DRIVER_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
    public string? CHR_BUS_CODE { get; set; }
    public string? CHR_VIOLATION_TYPE { get; set; } // đi muộn, thái độ, vi phạm luật,...
    public string? NVCHR_DESCRIPTION { get; set; }
    public DateTime? DTM_VIOLATION_DATE { get; set; }
}
