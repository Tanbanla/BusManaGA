using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_EVALUATION")]
public partial class TM_BUS_EVALUATION : AuditableEntity
{
    public int? INT_DRIVER_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
    public string? CHR_BUS_CODE { get; set; }
    public int? INT_RATING { get; set; }
    public string? NVCHR_COMMENT { get; set; }
    public string? CHR_EVALUATION_TYPE { get; set; }
    public DateTime? DTM_EVALUATION_DATE { get; set; }
}
