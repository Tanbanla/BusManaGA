using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.QltcManager;

public class QltcManagerPageViewModel
{
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? SuggestedAddress { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool CanManage { get; set; }

    public List<TM_EMPLOYEEDTO> Employees { get; set; } = new();
    public List<TM_SECTIONDTO> Sections { get; set; } = new();
    public List<TM_CITYDTO> Cities { get; set; } = new();

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

}
