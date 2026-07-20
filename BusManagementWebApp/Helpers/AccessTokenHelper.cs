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

                // helper to try multiple aliases
                static string Lookup(System.Collections.Generic.Dictionary<string, string> dict, params string[] keys)
                {
                    foreach (var k in keys)
                    {
                        if (string.IsNullOrWhiteSpace(k)) continue;
                        if (dict.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)) return v!;
                    }
                    return string.Empty;
                }

                var user = new CurrentUser
                {
                    EmployeeId = TryParseInt(Lookup(claims, System.Security.Claims.ClaimTypes.NameIdentifier, "sub", "employee_id", "employeeid")),
                    Name = Lookup(claims, System.Security.Claims.ClaimTypes.Name, "name", "preferred_username", "unique_name"),
                    EmployeeCode = Lookup(claims, "employee_code", "emp", "employee"),
                    Adid = Lookup(claims, "adid", System.Security.Claims.ClaimTypes.NameIdentifier, "upn", "preferred_username", "unique_name"),
                    AgentEmployeeId = Lookup(claims, "agent_employee_id"),
                    AgentName = Lookup(claims, "agent_name"),
                    Email = Lookup(claims, System.Security.Claims.ClaimTypes.Email, "email", "mail"),
                    Position = Lookup(claims, "position"),
                    PositionGroup = Lookup(claims, "position_group"),
                    Group = Lookup(claims, "group"),
                    DeptCode = Lookup(claims, "dept_code"),
                    DeptName = Lookup(claims, "dept_name"),
                    SecCode = Lookup(claims, "sec_code"),
                    SecName = Lookup(claims, "sec_name"),
                    CostCenterCode = Lookup(claims, "cost_center_code"),
                    CostCenterName = Lookup(claims, "cost_center_name"),
                    TeamId = TryParseInt(Lookup(claims, "team_id")),
                    IsLeader = TryParseBool(Lookup(claims, "is_leader", "isLeader", "leader")),
                    EmployeeJoinDate = TryParseDate(Lookup(claims, "employee_join_date")),
                    EmployeeLeaveDate = TryParseDate(Lookup(claims, "employee_leave_date"))
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
