using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_MASTER_MAIL")]
public partial class TM_MASTER_MAIL : AuditableEntity
{
    public string? CHR_MAIL_CODE { get; set; }
    public string? CHR_FROM { get; set; }
    public string? CHR_TO { get; set; }
    public string? CHR_CC { get; set; }
    public string? CHR_BCC { get; set; }
    public string? CHR_SUBJECT { get; set; }
    public string? CHR_CONTENT { get; set; }
    public string? CHR_ATTACH { get; set; }
    public bool? BIT_HIGHT { get; set; }
    public string? CHR_NOTE { get; set; }
}
