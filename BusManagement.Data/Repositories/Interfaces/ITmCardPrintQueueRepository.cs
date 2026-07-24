using BusManagement.Model;
using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmCardPrintQueueRepository : IBaseRepository<TM_CARD_PRINT_QUEUE, int>
{
    // search card print queue with pagination
    Task<NotCardViewModel> SearchCardPrintQueueAsync(SearchCard searchCard);
    // Delete card print queue 
    Task<bool> DeleteCardPrintQueuesAsync(List<int> ids);
    // Print card queue 
    Task<bool> PrintCardQueuesAsync(List<int> ids, string user);
}
