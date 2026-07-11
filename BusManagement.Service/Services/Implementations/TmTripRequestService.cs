using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmTripRequestService : BaseService<TM_TRIP_REQUEST, int, TM_TRIP_REQUESTDTO>, ITmTripRequestService
{
    private readonly ITmTripRequestRepository _repo;

    public TmTripRequestService(ITmTripRequestRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
