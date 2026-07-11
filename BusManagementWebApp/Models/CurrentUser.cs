using System;

namespace BusManagement.Models
{
    public class CurrentUser
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string EmployeeCode { get; set; } = string.Empty;
        public string Adid { get; set; } = string.Empty;
        public string AgentEmployeeId { get; set; } = string.Empty;
        public string AgentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string PositionGroup { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string DeptCode { get; set; } = string.Empty;
        public string DeptName { get; set; } = string.Empty;
        public string SecCode { get; set; } = string.Empty;
        public string SecName { get; set; } = string.Empty;
        public string CostCenterCode { get; set; } = string.Empty;
        public string CostCenterName { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public bool IsLeader { get; set; }
        public DateTime? EmployeeJoinDate { get; set; }
        public DateTime? EmployeeLeaveDate { get; set; }
    }
}
