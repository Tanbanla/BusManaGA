using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmVendorService : BaseService<TM_VENDOR, int, TM_VENDORDTO>, ITmVendorService
{
    private readonly ITmVendorRepository _repo;

    public TmVendorService(ITmVendorRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
