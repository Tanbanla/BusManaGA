using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.RequestStatusReport
{
    public class RequestStatusReportPageViewModel
    {
        public string? SelectedRequestTypeIds { get; set; }
        public string? EmployeeCode { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public List<int> SelectedRequestTypeIdList { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public List<RequestStatusRowViewModel> Requests { get; set; } = new();
        public List<TM_REQUEST_TYPEDTO> RequestTypes { get; set; } = new();
    }
}
