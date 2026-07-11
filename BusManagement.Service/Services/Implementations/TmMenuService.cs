using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmMenuService : BaseService<TM_MENU, int, TM_MENUDTO>, ITmMenuService
{
    private readonly ITmMenuRepository _repo;

    public TmMenuService(ITmMenuRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
