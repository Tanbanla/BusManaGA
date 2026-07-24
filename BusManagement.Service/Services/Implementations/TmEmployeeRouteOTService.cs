using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmEmployeeRouteOTService : BaseService<TM_EMPLOYEE_ROUTE_OT, int, TM_EMPLOYEE_ROUTE_OTDTO>, ITmEmployeeRouteOTService
{
    private readonly ITmEmployeeRouteOTRepository _repo;

    public TmEmployeeRouteOTService(ITmEmployeeRouteOTRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
