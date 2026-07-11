using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmEmployeeRouteService : BaseService<TM_EMPLOYEE_ROUTE, int, TM_EMPLOYEE_ROUTEDTO>, ITmEmployeeRouteService
{
    private readonly ITmEmployeeRouteRepository _repo;

    public TmEmployeeRouteService(ITmEmployeeRouteRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
