using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmEmployeeService : BaseService<TM_EMPLOYEE, int, TM_EMPLOYEEDTO>, ITmEmployeeService
{
    private readonly ITmEmployeeRepository _repo;

    public TmEmployeeService(ITmEmployeeRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
