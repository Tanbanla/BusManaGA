using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
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
    // list card taxi brorrowing
    public async Task<GenericResponse<List<TM_CARD_BORROWDTO>>> GetListCardUsingTaxiAsync()
    {
        var result = new GenericResponse<List<TM_CARD_BORROWDTO>>();
        try
        {
            var data = await _repo.GetListCardUsingTaxiAsync();
            result.Data = _mapper.Map<List<TM_CARD_BORROWDTO>>(data);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = 500;
            result.Message = ex.Message;
        }
        return result;
    }
}
