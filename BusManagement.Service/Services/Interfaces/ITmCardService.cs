using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using System.Security.Principal;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmCardService : IBaseService<TM_CARD, int, TM_CARDDTO>
{
    // Search for cards by card taxi
    Task<GenericResponse<List<dynamic>>> GetCardByTaxiAsync(SearchCard serach);
    // Delete soft card Taxi by IDs
    Task<GenericResponse<bool>> DeleteCardByTaxiAsync(List<int> ids);
    // Tải Excel dữ liệu thẻ taxi
    Task<GenericResponse<byte[]>> DownloadExcelTaxiAsync(SearchCard serach);
    //List card taxi need usable
    Task<GenericResponse<List<TM_CARD>>> GetListCardTaxiUsableAsync();
    // Get list Card using
    Task<GenericResponse<UsingCardViewModel>> GetCardUsing(string? employeeId, string? department, int page = 1, int pageSize = 10);
    // update card buss
    Task<GenericResponse<bool>> UpdateCardBusAsync(TM_CARDDTO card);

    // delete
    Task<GenericResponse<bool>> DeleteCardAsync(int id);
    Task<GenericResponse<bool>> DeleteCardsAsync(List<int> ids);
}
