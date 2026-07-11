using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_DIAGRAM")]
public partial class TM_DIAGRAM : AuditableEntity
{
    public string? NVCHR_DIAGRAM_NAME { get; set; }
    public string? CHR_DIAGRAM_TYPE { get; set; }
    public string? CHR_LOCATION { get; set; }
    public string? CHR_FILE_PATH { get; set; }
    public int? INT_VERSION { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
