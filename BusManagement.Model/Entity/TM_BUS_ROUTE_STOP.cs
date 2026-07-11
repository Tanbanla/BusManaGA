using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_ROUTE_STOP")]
public partial class TM_BUS_ROUTE_STOP
{
    public int ID { get; set; }
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_STOP_ID { get; set; }
    public int? INT_ORDER { get; set; }
    public DateTime? DTM_PICKUP_TIME { get; set; }
}
