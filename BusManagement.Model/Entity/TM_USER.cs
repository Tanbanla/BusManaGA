using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_USER")]
public partial class TM_USER : AuditableEntity
{
    public string? CHR_EMPLOYEE_ID { get; set; }
    public string? CHR_ADID { get; set; }
    public string? NVCHR_NAME { get; set; }
    public string? CHR_PASSWORD { get; set; }
    public string? CHR_SEC_CODE { get; set; }
    public string? CHR_SEC_NAME { get; set; }
    public string? CHR_POSITION { get; set; }
    public int? INT_ROLE_ID { get; set; }
    public DateTime? DTM_LAST_LOGIN { get; set; }
    public int? INT_LOCK { get; set; }
    public int? INT_LOCK_DAY { get; set; }
    public string? CHR_NOTE { get; set; }
    public string? CHR_AVATAR_PATH { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
