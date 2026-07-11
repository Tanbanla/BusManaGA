using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmTaxiService : BaseService<TM_TAXI, int, TM_TAXIDTO>, ITmTaxiService
{
    private readonly ITmTaxiRepository _repo;

    public TmTaxiService(ITmTaxiRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
