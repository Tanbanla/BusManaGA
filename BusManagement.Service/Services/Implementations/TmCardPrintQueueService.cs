using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmCardPrintQueueService : BaseService<TM_CARD_PRINT_QUEUE, int, TM_CARD_PRINT_QUEUEDTO>, ITmCardPrintQueueService
{
    private readonly ITmCardPrintQueueRepository _repo;

    public TmCardPrintQueueService(ITmCardPrintQueueRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
    // search card print queue with pagination
    public async Task<GenericResponse<NotCardViewModel>> SearchCardPrintQueueAsync(SearchCard searchCard)
    {
        var result = new GenericResponse<NotCardViewModel>();

        try
        {
            result.Data = await _repo.SearchCardPrintQueueAsync(searchCard);
            result.Success = true;
            result.Status = 200;

        }
        catch (Exception ex)
        {
            result.Status = 500;
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
    // Delete card print queue 
    public async Task<GenericResponse<bool>> DeleteCardPrintQueuesAsync(List<int> ids)
    {
        var result = new GenericResponse<bool>();

        try
        {
            result.Data = await _repo.DeleteCardPrintQueuesAsync(ids);
            result.Success = true;
            result.Status = 200;
        }
        catch (Exception ex)
        {
            result.Status = 500;
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
    // Print card queue 
    public async Task<GenericResponse<bool>> PrintCardQueuesAsync(List<int> ids, string user)
    {
        var result = new GenericResponse<bool>();

        try
        {
            result.Data = await _repo.PrintCardQueuesAsync(ids, user);
            result.Success = true;
            result.Status = 200;
        }
        catch (Exception ex)
        {
            result.Status = 500;
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
}