using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using BusManagement.Models;

namespace BusManagement.Helpers
{
    public static class AccessTokenHelper
    {
        public static CurrentUser? FromJwt(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var claims = jwt.Claims.ToDictionary(c => c.Type, c => c.Value ?? string.Empty);

                var user = new CurrentUser
                {
                    EmployeeId = TryParseInt(Get(claims, System.Security.Claims.ClaimTypes.NameIdentifier)),
                    Name = Get(claims, System.Security.Claims.ClaimTypes.Name),
                    EmployeeCode = Get(claims, "employee_code"),
                    Adid = Get(claims, "adid"),
                    AgentEmployeeId = Get(claims, "agent_employee_id"),
                    AgentName = Get(claims, "agent_name"),
                    Email = Get(claims, System.Security.Claims.ClaimTypes.Email),
                    Position = Get(claims, "position"),
                    PositionGroup = Get(claims, "position_group"),
                    Group = Get(claims, "group"),
                    DeptCode = Get(claims, "dept_code"),
                    DeptName = Get(claims, "dept_name"),
                    SecCode = Get(claims, "sec_code"),
                    SecName = Get(claims, "sec_name"),
                    CostCenterCode = Get(claims, "cost_center_code"),
                    CostCenterName = Get(claims, "cost_center_name"),
                    TeamId = TryParseInt(Get(claims, "team_id")),
                    IsLeader = TryParseBool(Get(claims, "is_leader")),
                    EmployeeJoinDate = TryParseDate(Get(claims, "employee_join_date")),
                    EmployeeLeaveDate = TryParseDate(Get(claims, "employee_leave_date"))
                };

                return user;
            }
            catch
            {
                return null;
            }
        }

        public static CurrentUser? FromHttpContext(HttpContext http)
        {
            if (http == null) return null;
            var token = http.Request.Cookies["AccessToken"];
            return FromJwt(token);
        }

        private static string Get(System.Collections.Generic.Dictionary<string, string> dict, string key)
        {
            if (dict == null) return string.Empty;
            return dict.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;
        }

        private static int TryParseInt(string s)
        {
            if (int.TryParse(s, out var v)) return v;
            return 0;
        }

        private static bool TryParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (bool.TryParse(s, out var b)) return b;
            // accept "1"/"0"
            if (s == "1") return true;
            if (s == "0") return false;
            return false;
        }

        private static DateTime? TryParseDate(string s)
        {
            if (DateTime.TryParse(s, out var d)) return d;
            return null;
        }
    }
}
