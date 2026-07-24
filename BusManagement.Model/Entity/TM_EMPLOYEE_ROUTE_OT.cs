using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_EMPLOYEE_ROUTE_OT")]
public partial class TM_EMPLOYEE_ROUTE_OT : AuditableEntity
{
    public int INT_EMPLOYEE_ID { get; set; }
    [StringLength(50)]
    public string CHR_EMPLOYEE_ID { get; set; } = string.Empty;
    public int? INT_BUS_OT_ASSIGN_ID { get; set; } // Tuyến xe OT đang hoạt động
    public int? INT_BUS_STOP_ID { get; set; } = 1; // Điểm đón/ trả xe OT
    public bool BIT_IS_ACTIVE { get; set; }
}
