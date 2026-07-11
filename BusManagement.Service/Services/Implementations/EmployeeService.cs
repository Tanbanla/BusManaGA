using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Implementations;
using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Service.Services.Interfaces;
using BusManagement.Model.Models;
using Microsoft.EntityFrameworkCore;

namespace BusManagement.Service.Services.Implementations;

public class EmployeeService : BaseService<Model.Entity.TM_EMPLOYEE, int, TM_EMPLOYEEDTO>, IEmployeeService
{
    private readonly IEmployeeRepository _repo;
    public EmployeeService(IEmployeeRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }

    public async Task<Model.Models.TM_EMPLOYEE?> GetAgentEmployeeByAdidAsync(string adid)
    {
        if (string.IsNullOrEmpty(adid)) return null;
        return await _repo.GetAgentEmployeeByEmployeeADIDAsync(adid);
    }

    public async Task<Model.Models.TM_EMPLOYEE?> GetAgentEmployeeByEmployeeIdAsync(string employeeId)
    {
        if (string.IsNullOrWhiteSpace(employeeId)) return null;
        return await _repo.GetAgentEmployeeByEmployeeIdAsync(employeeId);
    }

    public async Task<Model.Entity.TM_EMPLOYEE> EnsureLocalEmployeeFromAgentAsync(Model.Models.TM_EMPLOYEE agentEmployee)
    {
        if (agentEmployee == null || string.IsNullOrEmpty(agentEmployee.CHR_EMPLOYEE_ID))
            throw new ArgumentException("Invalid agent employee data");

        // Try to find existing local employee by CHR_EMPLOYEE_ID
        var queryable = await _repo.GetQueryableAsync();
        var existing = await queryable.FirstOrDefaultAsync(e => e.CHR_EMPLOYEE_ID == agentEmployee.CHR_EMPLOYEE_ID);
        if (existing != null)
        {
            return existing;
        }

        // Map TM_EMPLOYEE_AGENT -> TM_EMPLOYEE
        var local = _mapper.Map<Model.Entity.TM_EMPLOYEE>(agentEmployee);

        // Ensure required fields
        if (string.IsNullOrEmpty(local.CHR_EMPLOYEE_ID)) local.CHR_EMPLOYEE_ID = agentEmployee.CHR_EMPLOYEE_ID ?? string.Empty;
        if (string.IsNullOrEmpty(local.NVCHR_EMPLOYEE_NAME)) local.NVCHR_EMPLOYEE_NAME = agentEmployee.CHR_EMPLOYEE_NAME ?? string.Empty;

        // Create new
        var created = await _repo.AddAsyncV2(local);
        return created;
    }

    public async Task<Model.Entity.TM_EMPLOYEE?> GetByEmployeeCodeAsync(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode)) return null;
        var q = await _repo.GetQueryableAsync();
        return await q.FirstOrDefaultAsync(e => e.CHR_EMPLOYEE_ID == employeeCode);
    }
}
