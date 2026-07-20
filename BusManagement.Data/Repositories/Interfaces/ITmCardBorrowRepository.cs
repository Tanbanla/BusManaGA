using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmCardBorrowRepository : IBaseRepository<TM_CARD_BORROW, int>
{
    // list card taxi brorrowing
    Task<List<TM_CARD_BORROW>> GetListCardUsingTaxiAsync();
}
