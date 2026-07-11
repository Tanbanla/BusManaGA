using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmBusEvaluationService : BaseService<TM_BUS_EVALUATION, int, TM_BUS_EVALUATIONDTO>, ITmBusEvaluationService
{
    private readonly ITmBusEvaluationRepository _repo;

    public TmBusEvaluationService(ITmBusEvaluationRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
