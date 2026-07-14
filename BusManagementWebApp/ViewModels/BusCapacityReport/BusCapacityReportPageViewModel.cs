using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.BusCapacityReport
{
    public class BusCapacityReportPageViewModel
    {
        public string? EmployeeCode { get; set; }
        public string? DepartmentCode { get; set; }
        public string? VehicleType { get; set; }
        public int? MinCapacity { get; set; }
        public int? MaxCapacity { get; set; }
        public int? MinUsed { get; set; }
        public int? MaxUsed { get; set; }
        public int? MinAvailable { get; set; }
        public int? MaxAvailable { get; set; }
        public string? Note { get; set; }
        public bool? ShowFullCapacity { get; set; }
        public bool? ShowAvailableOnly { get; set; }

        public List<BusCapacityReportRowViewModel> Reports { get; set; } = new();
        public List<TM_SECTIONDTO> Departments { get; set; } = new();
    }
}
