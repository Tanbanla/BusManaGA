using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmCityService : BaseService<TM_CITY, int, TM_CITYDTO>, ITmCityService
{
    private readonly ITmCityRepository _repo;

    public TmCityService(ITmCityRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
