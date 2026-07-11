using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusRouteService : BaseService<TM_BUS_ROUTE, int, TM_BUS_ROUTEDTO>, ITmBusRouteService
{
    private readonly ITmBusRouteRepository _repo;

    public TmBusRouteService(ITmBusRouteRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
