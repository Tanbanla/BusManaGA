using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.EmployeeBus;

public class EmployeeBusPageViewModel
{
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Ward { get; set; }
    public int? BusStopId { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool CanManage { get; set; }

    public List<TM_EMPLOYEEDTO> Employees { get; set; } = new();
    public List<TM_BUS_STOPDTO> BusStops { get; set; } = new();
    public List<TM_SECTIONDTO> Sections { get; set; } = new();
    public List<TM_CITYDTO> Cities { get; set; } = new();
    public List<TM_WARDDTO> Wards { get; set; } = new();

    public string GetDepartmentName(string? sectionCode)
    {
        if (string.IsNullOrWhiteSpace(sectionCode))
        {
            return string.Empty;
        }

        var section = Sections.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.CHR_CODE)
            && string.Equals(x.CHR_CODE.Trim(), sectionCode.Trim(), StringComparison.OrdinalIgnoreCase));

        return section?.CHR_NAME
            ?? section?.CHR_SECTION_NAME
            ?? sectionCode;
    }

    public string GetBusStopDisplayName(int? busStopId)
    {
        if (!busStopId.HasValue || busStopId.Value <= 0)
        {
            return string.Empty;
        }

        var busStop = BusStops.FirstOrDefault(x => x.ID == busStopId.Value);
        if (busStop == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(busStop.NVCHR_STOP_NAME))
        {
            return $"{busStop.NVCHR_STOP_NAME}";
        }

        return busStop.NVCHR_STOP_NAME
            ?? busStop.NVCHR_STOP_NAME
            ?? string.Empty;
    }
}
