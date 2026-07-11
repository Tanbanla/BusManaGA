using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmEmployeeRouteHistoryService : BaseService<TM_EMPLOYEE_ROUTE_HISTORY, int, TM_EMPLOYEE_ROUTE_HISTORYDTO>, ITmEmployeeRouteHistoryService
{
    private readonly ITmEmployeeRouteHistoryRepository _repo;

    public TmEmployeeRouteHistoryService(ITmEmployeeRouteHistoryRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
