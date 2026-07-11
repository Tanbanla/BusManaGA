namespace BusManagement.Model
{
    public class SearchCard
    {
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? SectionCode { get; set; }
        public string? TaxiCardNumber { get; set; }
        public string? TaxiCompany { get; set; }
        public string? AddressTo { get; set; }
        public string? AddressFrom { get; set; }
        public DateTime? DateBorow { get; set; }
        public DateTime? DateReturn { get; set; }
        public int? Number { get; set; }
        public string? CostSum { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }

    }
}
