using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusService : BaseService<TM_BUS, int, TM_BUSDTO>, ITmBusService
{
    private readonly ITmBusRepository _repo;

    public TmBusService(ITmBusRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
    // get status Activition 
    public async Task<GenericResponse<List<TM_BUS>>> GetActiveBusesAsync()
    {
        var result = new GenericResponse<List<TM_BUS>>();
        try
        {
            var buses = await _repo.GetActiveBusesAsync();
            result.Data = buses;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }
        return result;
    }
}