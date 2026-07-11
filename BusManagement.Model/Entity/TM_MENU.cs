using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_MENU")]
public partial class TM_MENU : AuditableEntity
{
    public string? CHR_MENU_CODE { get; set; }
    public string? NVCHR_MENU_NAME { get; set; }
    public int? INT_PARENT_ID { get; set; }
    public string? CHR_CONTROLLER { get; set; }
    public string? CHR_ACTION { get; set; }
    public string? CHR_ICON { get; set; }
    public int? INT_SORT_ORDER { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
