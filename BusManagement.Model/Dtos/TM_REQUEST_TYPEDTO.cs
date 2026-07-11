namespace BusManagement.Model.Dtos;

public partial class TM_REQUEST_TYPEDTO : AuditableDto
{
    public string? NVCHR_REQUEST_TYPE { get; set; }
    public int? INT_GROUP { get; set; }
}
