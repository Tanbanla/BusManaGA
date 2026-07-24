using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusStopService : BaseService<TM_BUS_STOP, int, TM_BUS_STOPDTO>, ITmBusStopService
{
    private readonly ITmBusStopRepository _repo;

    public TmBusStopService(ITmBusStopRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
    // get bus stop by Active status
    public async Task<GenericResponse<List<TM_BUS_STOP>>> GetActiveBusStopsAsync()
    {
        try
        {
            var result = await _repo.GetActiveBusStopsAsync();
            return new GenericResponse<List<TM_BUS_STOP>>
            {
                Success = true,
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new GenericResponse<List<TM_BUS_STOP>>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
}