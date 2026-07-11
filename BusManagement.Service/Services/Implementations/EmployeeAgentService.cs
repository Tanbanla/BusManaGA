using AutoMapper;
using BusManagement.Data.Repositories.Implementations;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using BusManagement.Service.Services.Implementations;
using BusManagement.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusManagement.Service.Services.Implementations
{
    public class EmployeeAgentService : BaseService<Model.Models.TM_EMPLOYEE, int, TM_EMPLOYEE_AGENTDTO>, IEmployeeAgentService
    {
        private readonly IEmployeeAgentRepository _repo;

        public EmployeeAgentService(IEmployeeAgentRepository repository, IMapper mapper) : base(repository, mapper)
        {
            _repo = repository;
        }

        public async Task<GenericResponse<IEnumerable<TM_EMPLOYEE_AGENTDTO>>> GetByAdidAsync(string adid)
        {
            var response = new GenericResponse<IEnumerable<TM_EMPLOYEE_AGENTDTO>>();
            try
            {
                var entities = await _repo.GetByAdidAsync(adid);
                response.Data = _mapper.Map<IEnumerable<TM_EMPLOYEE_AGENTDTO>>(entities);
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<GenericResponse<PagedList<TM_EMPLOYEE_AGENTDTO>>> SearchByDuplicateAdidAsync(SearchOptions options)
        {
            var response = new GenericResponse<PagedList<TM_EMPLOYEE_AGENTDTO>>();
            try
            {
                if (options.PageNumber ==0) options.PageNumber =1;
                if (options.PageSize ==0) options.PageSize =10;
                var result = await _repo.SearchByDuplicateAdidAsync(options);
                var dtoPaged = new PagedList<TM_EMPLOYEE_AGENTDTO>
                {
                    PageIndex = result.PageIndex,
                    PageSize = result.PageSize,
                    TotalPages = result.TotalPages,
                    TotalFilter = result.TotalFilter,
                    TotalCount = result.TotalCount,
                    Data = _mapper.Map<List<TM_EMPLOYEE_AGENTDTO>>(result.Data)
                };
                response.Data = dtoPaged;
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<GenericResponse<TM_EMPLOYEE_AGENTDTO>> GetByEmployeeIdAsync(string employeeId)
        {
            var response = new GenericResponse<TM_EMPLOYEE_AGENTDTO>();
            try
            {
                var entity = await _repo.GetByEmployeeIdAsync(employeeId);
                response.Data = _mapper.Map<TM_EMPLOYEE_AGENTDTO>(entity);
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<GenericResponse<List<TM_EMPLOYEE_AGENTDTO>>> GetQLSCApproverBySection(string? section)
        {
            var respone = new GenericResponse<List<TM_EMPLOYEE_AGENTDTO>>();
            try
            {
                respone.Data = await _repo.GetQLSCApproverBySection(section);
                respone.Success = true;
            }
            catch (Exception ex)
            {
                respone.Success = false;
                respone.Message = ex.Message;
            }
            return respone;
        }

        public async Task<GenericResponse<List<TM_EMPLOYEE_AGENTDTO>>> GetQLTCApproverBySection(string? section)
        {
            var respone = new GenericResponse<List<TM_EMPLOYEE_AGENTDTO>>();
            try
            {
                respone.Data = await _repo.GetQLTCApproverBySection(section);
                respone.Success = true;
            }
            catch (Exception ex)
            {
                respone.Success = false;
                respone.Message = ex.Message;
            }
            return respone;
        }
    }
}
