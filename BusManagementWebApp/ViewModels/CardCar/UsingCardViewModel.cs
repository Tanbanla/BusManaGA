using BusManagement.Model.Dtos;

namespace BusManagementWebApp.ViewModels.CardCar
{
    public class UsingCardViewModel
    {
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages { get; set; }

        public List<dynamic> Cards { get; set; } = new();
    }
}
