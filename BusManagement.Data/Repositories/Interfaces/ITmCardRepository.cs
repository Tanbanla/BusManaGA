using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmCardRepository : IBaseRepository<TM_CARD, int>
{
    // Search for cards by card taxi
    Task<List<dynamic>> GetCardByTaxiAsync(SearchCard serach);
    // Delete soft card Taxi by IDs
    Task<bool> DeleteCardByTaxiAsync(List<int> ids);
    //List card taxi need usable
    Task<List<TM_CARD>> GetListCardTaxiUsableAsync();
    // Get list Card using
    Task<UsingCardViewModel> GetCardUsing(string? employeeId, string? department, int page = 1, int pageSize = 10);
    // Update card buss
    Task<bool> UpdateCardBusAsync(TM_CARD card);

    // delete
    Task<bool> DeleteCardAsync(int id);
    Task<bool> DeleteCardsAsync(List<int> ids);
}
