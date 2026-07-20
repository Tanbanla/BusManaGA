using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmCardBorrowService : IBaseService<TM_CARD_BORROW, int, TM_CARD_BORROWDTO>
{
    // list card taxi brorrowing
    Task<GenericResponse<List<TM_CARD_BORROWDTO>>> GetListCardUsingTaxiAsync();
}
