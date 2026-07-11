using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_REQUEST_TYPE")]
public partial class TM_REQUEST_TYPE : AuditableEntity
{
    public string? NVCHR_REQUEST_TYPE { get; set; }
    public int? INT_GROUP { get; set; }
}
