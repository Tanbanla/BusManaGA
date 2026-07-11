using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
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
}
