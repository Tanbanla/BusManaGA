using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmMenuRoleService : BaseService<TM_MENU_ROLE, int, TM_MENU_ROLEDTO>, ITmMenuRoleService
{
    private readonly ITmMenuRoleRepository _repo;

    public TmMenuRoleService(ITmMenuRoleRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
