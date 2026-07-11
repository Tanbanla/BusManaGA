using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmRoleService : BaseService<TM_ROLE, int, TM_ROLEDTO>, ITmRoleService
{
    private readonly ITmRoleRepository _repo;

    public TmRoleService(ITmRoleRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
