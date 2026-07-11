using BusManagement.Model.Dtos;
using BusManagement.Model.Models;
using BusManagement.Service.Services.Implementations;
using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class MasterPositionService : BaseService<TM_MASTER_POSITION, int, TM_MASTER_POSITIONDTO>, IMasterPositionService
{
    private readonly IMasterPositionRepository _repo;
    public MasterPositionService(IMasterPositionRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
