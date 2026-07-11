using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_OT")]
public partial class TM_BUS_OT : AuditableEntity
{
    public DateTime? DTM_DATE { get; set; }
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
}
