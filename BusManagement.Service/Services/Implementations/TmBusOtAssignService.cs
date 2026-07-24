using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusOtAssignService : BaseService<TM_BUS_OT_ASSIGN, int, TM_BUS_OT_ASSIGNDTO>, ITmBusOtAssignService
{
    private readonly ITmBusOtAssignRepository _repo;
        
    public TmBusOtAssignService(ITmBusOtAssignRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
