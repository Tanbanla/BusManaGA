using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmDriverViolationService : BaseService<TM_DRIVER_VIOLATION, int, TM_DRIVER_VIOLATIONDTO>, ITmDriverViolationService
{
    private readonly ITmDriverViolationRepository _repo;

    public TmDriverViolationService(ITmDriverViolationRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
