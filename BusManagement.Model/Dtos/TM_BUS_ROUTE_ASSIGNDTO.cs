namespace BusManagement.Model.Dtos;

public partial class TM_BUS_ROUTE_ASSIGNDTO : AuditableDto
{
    public string? NVCHR_BUS_ROUTE_WORKING_NAME { get; set; }
    public int? INT_ROUTE_PICKUP_ID { get; set; }
    public int? INT_ROUTE_RETURN_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
    public int? INT_DRIVER_ID { get; set; }
    public int? INT_CURRENT_USER { get; set; }
    public string? CHR_BRA_TYPE { get; set; }
    public int? INT_CAPACITY { get; set; } // dung lượng xe công nhân
    public DateTime? DTM_EFFECTIVE_FROM { get; set; }
    public DateTime? DTM_EFFECTIVE_TO { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
