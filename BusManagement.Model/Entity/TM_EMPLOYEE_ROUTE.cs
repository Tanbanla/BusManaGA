using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_EMPLOYEE_ROUTE")]
public partial class TM_EMPLOYEE_ROUTE : AuditableEntity
{
    public int INT_EMPLOYEE_ID { get; set; }
    [StringLength(50)]
    public string CHR_EMPLOYEE_ID { get; set; } = string.Empty;
    public int INT_ROUTE_ASSIGN_ID { get; set; }
    public int INT_PICKUP_STOP_ID { get; set; }
    public int INT_RETURN_STOP_ID { get; set; }
    public DateTime DTM_EFFECTIVE_FROM { get; set; }
    public DateTime? DTM_EFFECTIVE_TO { get; set; }
    public bool BIT_IS_ACTIVE { get; set; }
}
