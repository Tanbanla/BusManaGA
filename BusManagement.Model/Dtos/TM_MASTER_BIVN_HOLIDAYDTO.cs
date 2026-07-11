namespace BusManagement.Model.Dtos;

public partial class TM_MASTER_BIVN_HOLIDAYDTO
{
    public decimal ID { get; set; }

    public DateTime DTM_NO_WORKING { get; set; }

    public string? CHR_REMARK { get; set; }

    public string? CHR_HOLIDAY_JP { get; set; }

    public int? INT_FINANCIAL_YEAR { get; set; }
}
