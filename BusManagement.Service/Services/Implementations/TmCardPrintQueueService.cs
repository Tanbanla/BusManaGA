using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmCardPrintQueueService : BaseService<TM_CARD_PRINT_QUEUE, int, TM_CARD_PRINT_QUEUEDTO>, ITmCardPrintQueueService
{
    private readonly ITmCardPrintQueueRepository _repo;

    public TmCardPrintQueueService(ITmCardPrintQueueRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
