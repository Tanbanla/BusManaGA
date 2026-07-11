using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusService : BaseService<TM_BUS, int, TM_BUSDTO>, ITmBusService
{
    private readonly ITmBusRepository _repo;

    public TmBusService(ITmBusRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
