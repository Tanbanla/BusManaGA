using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmRequestTypeService : BaseService<TM_REQUEST_TYPE, int, TM_REQUEST_TYPEDTO>, ITmRequestTypeService
{
    private readonly ITmRequestTypeRepository _repo;

    public TmRequestTypeService(ITmRequestTypeRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
