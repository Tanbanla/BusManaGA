using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmApprovalHistoryService : BaseService<TM_APPROVAL_HISTORY, int, TM_APPROVAL_HISTORYDTO>, ITmApprovalHistoryService
{
    private readonly ITmApprovalHistoryRepository _repo;

    public TmApprovalHistoryService(ITmApprovalHistoryRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
