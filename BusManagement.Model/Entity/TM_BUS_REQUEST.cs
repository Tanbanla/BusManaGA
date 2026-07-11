using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_REQUEST")]
public partial class TM_BUS_REQUEST : AuditableEntity
{
    public string? CHR_EMPLOYEE_ID { get; set; }
    public int? INT_BUS_ROUTE_ASSIGN_ID { get; set; }
    public int? INT_BUS_STOP_ID { get; set; }
    public string? CHR_NOTE { get; set; }
    public DateTime? DTM_START_DATE { get; set; }
    public DateTime? DTM_CANCEL_DATE { get; set; }
    public int? INT_REQUEST_TYPE { get; set; }
    public int? INT_STATUS { get; set; }
    public string? CHR_QLSC_APPROVE { get; set; }
    public string? CHR_GA_CONFIRM { get; set; }
}
