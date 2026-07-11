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
}
