using BusManagement.Model.Dtos;

namespace BusManagementWebApp.ViewModels.CardCar
{
    public class CardCarPageVM
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public List<TM_SECTIONDTO> Sections { get; set; } = new();
    }
}
