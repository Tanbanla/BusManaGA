using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmCardBorrowService : BaseService<TM_CARD_BORROW, int, TM_CARD_BORROWDTO>, ITmCardBorrowService
{
    private readonly ITmCardBorrowRepository _repo;

    public TmCardBorrowService(ITmCardBorrowRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
