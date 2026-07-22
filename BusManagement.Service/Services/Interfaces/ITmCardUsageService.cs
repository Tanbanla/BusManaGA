using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmCardUsageService : IBaseService<TM_CARD_USAGE, int, TM_CARD_USAGEDTO>
{
    // update Card Usage by card id
    Task<GenericResponse<bool>> UpdateCardUsageByCardIdAsync(TM_CARD_USAGE cardUsage);
}
