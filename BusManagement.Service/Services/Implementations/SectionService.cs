using BusManagement.Model.Dtos;
using BusManagement.Model.Models;
using BusManagement.Service.Services.Implementations;
using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class SectionService : BaseService<TM_SECTION, int, TM_SECTIONDTO>, ISectionService
{
    private readonly ISectionRepository _repo;
    public SectionService(ISectionRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
