using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmCardUsageRepository : IBaseRepository<TM_CARD_USAGE, int>
{
    // update Card Usage by card id
    Task<bool> UpdateCardUsageByCardIdAsync(TM_CARD_USAGE cardUsage);
}
