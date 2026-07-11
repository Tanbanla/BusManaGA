namespace BusManagement.Model.Dtos;

public partial class TM_ROLEDTO : AuditableDto
{
    public string? NVCHR_ROLE_NAME { get; set; }
    public string? NVCHR_DESCRIPTION { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
