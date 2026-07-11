using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_ROUTE")]
public partial class TM_BUS_ROUTE : AuditableEntity
{
    public string? NVCHR_ROUTE_NAME { get; set; }
    public string? NVCHR_ROUTE_TYPE { get; set; }
    public string? CHR_CODE_DEPT { get; set; }
    public string? CHR_CODE_SEC { get; set; }
    public int? INT_ID_ROUTE_TYPE { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
