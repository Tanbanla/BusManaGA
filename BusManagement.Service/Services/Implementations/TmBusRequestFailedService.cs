using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusRequestFailedService : BaseService<TM_BUS_REQUEST_FAILED, int, TM_BUS_REQUEST_FAILEDDTO>, ITmBusRequestFailedService
{
    private readonly ITmBusRequestFailedRepository _repo;

    public TmBusRequestFailedService(ITmBusRequestFailedRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
