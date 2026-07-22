using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_ROUTE_STOP")]
public partial class TM_BUS_ROUTE_STOP
{
    public int ID { get; set; }
    public int? INT_TURN { get; set; } // Lượt 1, 2, 3, 4
    public string? NVCHR_TURN_NAME { get; set; }
    public int? INT_DIRECTION { get; set; } // Chiều đi/về
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_STOP_ID { get; set; }
    public int? INT_ORDER { get; set; }
    public DateTime? DTM_PICKUP_TIME { get; set; }
}
