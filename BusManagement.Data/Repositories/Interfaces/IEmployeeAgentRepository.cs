using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusManagement.Data.Repositories.Interfaces
{
    public interface IEmployeeAgentRepository : IBaseRepository<Model.Models.TM_EMPLOYEE, int>
    {
        Task<IEnumerable<Model.Models.TM_EMPLOYEE>> GetByAdidAsync(string adid);
        Task<Model.Models.TM_EMPLOYEE> GetByEmployeeIdAsync(string employeeId);
        Task<PagedList<Model.Models.TM_EMPLOYEE>> SearchByDuplicateAdidAsync(SearchOptions options);
        Task<List<TM_EMPLOYEE_AGENTDTO>> GetQLSCApproverBySection(string? sectionCode);
        Task<List<TM_EMPLOYEE_AGENTDTO>> GetQLTCApproverBySection(string? sectionCode);
    }
}
