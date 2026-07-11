namespace BusManagement.Model.Dtos;

public partial class TM_BUS_OTDTO : AuditableDto
{
    public DateTime? DTM_DATE { get; set; }
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
}
