using BusManagement.Model.Dtos;
using BusManagement.Model.Models;
using BusManagement.Service.Services.Interfaces;
using BusManagement.Model.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces
{
    public interface IEmployeeAgentService : IBaseService<Model.Models.TM_EMPLOYEE, int, TM_EMPLOYEE_AGENTDTO>
    {
        Task<GenericResponse<IEnumerable<TM_EMPLOYEE_AGENTDTO>>> GetByAdidAsync(string adid);
        Task<GenericResponse<TM_EMPLOYEE_AGENTDTO>> GetByEmployeeIdAsync(string employeeId);
        Task<GenericResponse<PagedList<TM_EMPLOYEE_AGENTDTO>>> SearchByDuplicateAdidAsync(SearchOptions options);
        Task<GenericResponse<List<TM_EMPLOYEE_AGENTDTO>>> GetQLSCApproverBySection(string? section);
        Task<GenericResponse<List<TM_EMPLOYEE_AGENTDTO>>> GetQLTCApproverBySection(string? section);
    }
}
