using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusOtService : BaseService<TM_BUS_OT, int, TM_BUS_OTDTO>, ITmBusOtService
{
    private readonly ITmBusOtRepository _repo;

    public TmBusOtService(ITmBusOtRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
