using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmCardUsageService : BaseService<TM_CARD_USAGE, int, TM_CARD_USAGEDTO>, ITmCardUsageService
{
    private readonly ITmCardUsageRepository _repo;

    public TmCardUsageService(ITmCardUsageRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
    // update Card Usage by card id
    public async Task<GenericResponse<bool>> UpdateCardUsageByCardIdAsync(TM_CARD_USAGE cardUsage)
    {
        var result = new GenericResponse<bool>();
        try
        {
            result.Data = await _repo.UpdateCardUsageByCardIdAsync(cardUsage);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = 500;
            result.Message = ex.Message;
        }
        return result;
    }
}
