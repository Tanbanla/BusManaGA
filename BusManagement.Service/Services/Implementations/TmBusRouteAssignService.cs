using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusRouteAssignService : BaseService<TM_BUS_ROUTE_ASSIGN, int, TM_BUS_ROUTE_ASSIGNDTO>, ITmBusRouteAssignService
{
    private readonly ITmBusRouteAssignRepository _repo;

    public TmBusRouteAssignService(ITmBusRouteAssignRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
