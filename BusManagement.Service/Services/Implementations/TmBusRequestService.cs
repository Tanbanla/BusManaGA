using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusRequestService : BaseService<TM_BUS_REQUEST, int, TM_BUS_REQUESTDTO>, ITmBusRequestService
{
    private readonly ITmBusRequestRepository _repo;

    public TmBusRequestService(ITmBusRequestRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
