using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusRouteStopService : BaseService<TM_BUS_ROUTE_STOP, int, TM_BUS_ROUTE_STOPDTO>, ITmBusRouteStopService
{
    private readonly ITmBusRouteStopRepository _repo;

    public TmBusRouteStopService(ITmBusRouteStopRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
