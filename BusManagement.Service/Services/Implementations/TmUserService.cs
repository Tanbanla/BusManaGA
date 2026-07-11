using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmUserService : BaseService<TM_USER, int, TM_USERDTO>, ITmUserService
{
    private readonly ITmUserRepository _repo;

    public TmUserService(ITmUserRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
