namespace BusManagement.Model.Dtos;

public partial class TM_BUS_ROUTE_STOPDTO : AuditableDto
{
    public int? INT_TURN { get; set; }
    public string? NVCHR_TURN_NAME { get; set; }
    public int? INT_DIRECTION { get; set; } // Chiều đi/về
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_STOP_ID { get; set; }
    public int? INT_ORDER { get; set; }
    public DateTime? DTM_PICKUP_TIME { get; set; }
}
