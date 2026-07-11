using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmDriverService : BaseService<TM_DRIVER, int, TM_DRIVERDTO>, ITmDriverService
{
    private readonly ITmDriverRepository _repo;

    public TmDriverService(ITmDriverRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
