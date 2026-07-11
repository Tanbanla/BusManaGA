using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmCardUsageService : BaseService<TM_CARD_USAGE, int, TM_CARD_USAGEDTO>, ITmCardUsageService
{
    private readonly ITmCardUsageRepository _repo;

    public TmCardUsageService(ITmCardUsageRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
