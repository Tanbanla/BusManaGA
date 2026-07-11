using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmWardService : BaseService<TM_WARD, int, TM_WARDDTO>, ITmWardService
{
    private readonly ITmWardRepository _repo;

    public TmWardService(ITmWardRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
