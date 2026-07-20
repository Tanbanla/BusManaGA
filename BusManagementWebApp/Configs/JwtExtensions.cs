using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;

namespace BusManagementWebApp.Configs
{
    public sealed class JwtOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenMinutes { get; set; } = 240;
        public bool? ValidateIssuer { get; set; }
        public bool? ValidateAudience { get; set; }
        public int ClockSkewSeconds { get; set; } = 60;
    }

    public static class JwtExtensions
    {
        private const string SectionName = "Jwt";

        public static IServiceCollection AddJwtAuth(this IServiceCollection services, IConfiguration configuration)
        {
            var section = configuration.GetSection(SectionName);
            var jwtOptions = section.Get<JwtOptions>() ?? new JwtOptions();

            if (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32)
                throw new InvalidOperationException("Jwt:Key phải được cấu hình và đủ mạnh (>= 32 ký tự).");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));
            bool validateIssuer = jwtOptions.ValidateIssuer ?? !string.IsNullOrWhiteSpace(jwtOptions.Issuer);
            bool validateAudience = jwtOptions.ValidateAudience ?? !string.IsNullOrWhiteSpace(jwtOptions.Audience);

            services
                .AddAuthentication(o =>
                {
                    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = validateIssuer,
                        ValidateAudience = validateAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = signingKey,
                        ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
                        // use 'adid' as primary name claim to align with app expectations
                        NameClaimType = "adid",
                        RoleClaimType = "role"
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtAuth");

                            var auth = ctx.Request.Headers["Authorization"].ToString();

                            if (!string.IsNullOrWhiteSpace(auth))
                            {
                                // Tách theo khoảng trống: ví dụ: Bearer Bearer eyJ...
                                var parts = auth.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                                // Tìm token hợp lệ:
                                // parts[0] phải là Bearer; token thường ở parts[1]
                                // Nếu parts[1] lại là 'Bearer' (do user dán thừa) thì lấy parts[2]
                                if (parts.Length >= 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                                {
                                    string candidate = parts[1];
                                    if (candidate.Equals("Bearer", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
                                    {
                                        candidate = parts[2];
                                        logger.LogInformation("Detected double 'Bearer' prefix. Cleaned.");
                                    }
                                    ctx.Token = candidate.Trim();
                                }
                            }

                            if (string.IsNullOrWhiteSpace(ctx.Token))
                            {
                                // Fallback query ?access_token=
                                var qToken = ctx.Request.Query["access_token"].ToString();
                                if (!string.IsNullOrWhiteSpace(qToken))
                                {
                                    ctx.Token = qToken.Trim();
                                    logger.LogInformation("Token taken from query parameter 'access_token'.");
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(ctx.Token))
                            {
                                var dotCount = ctx.Token.Count(c => c == '.');
                                if (dotCount != 2)
                                {
                                    logger.LogWarning("Extracted token not in compact JWT format (dotCount={DotCount}). Preview='{Preview}'",
                                        dotCount,
                                        ctx.Token.Length > 80 ? ctx.Token[..80] + "..." : ctx.Token);
                                }
                            }
                            else
                            {
                                logger.LogDebug("No bearer token found after normalization.");
                            }

                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtAuth");
                            logger.LogError(ctx.Exception, "JWT authentication failed");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = ctx =>
                        {
                            try
                            {
                                var ci = ctx.Principal?.Identity as ClaimsIdentity;
                                if (ci != null)
                                {
                                    // Normalize ADID claim: try common aliases and ensure 'adid' exists
                                    var adid = ci.FindFirst("adid")?.Value
                                        ?? ci.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                        ?? ci.FindFirst("sub")?.Value
                                        ?? ci.FindFirst("upn")?.Value
                                        ?? ci.FindFirst("preferred_username")?.Value
                                        ?? ci.FindFirst("unique_name")?.Value
                                        ?? ci.FindFirst("name")?.Value;

                                    if (!string.IsNullOrWhiteSpace(adid) && !ci.HasClaim(c => string.Equals(c.Type, "adid", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        ci.AddClaim(new Claim("adid", adid));
                                    }

                                    // Normalize employee_code claim
                                    var emp = ci.FindFirst("employee_code")?.Value
                                        ?? ci.FindFirst("emp")?.Value
                                        ?? ci.FindFirst("employee")?.Value;

                                    if (!string.IsNullOrWhiteSpace(emp) && !ci.HasClaim(c => string.Equals(c.Type, "employee_code", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        ci.AddClaim(new Claim("employee_code", emp));
                                    }
                                }
                            }
                            catch
                            {
                                // ignore normalization errors
                            }

                            return Task.CompletedTask;
                        },
                        OnChallenge = ctx =>
                        {
                            if (!ctx.Handled && !ctx.Response.HasStarted)
                            {
                                ctx.HandleResponse();
                                var logger = ctx.HttpContext.RequestServices
                                    .GetRequiredService<ILoggerFactory>()
                                    .CreateLogger("JwtAuth");
                                string failureMsg = ctx.AuthenticateFailure?.Message ??
                                                    ctx.ErrorDescription ??
                                                    ctx.Error ??
                                                    "unauthorized";
                                logger.LogWarning("JWT challenge (reason={Reason})", failureMsg);
                                ctx.Response.StatusCode = 401;
                                ctx.Response.ContentType = "application/json";
                                return ctx.Response.WriteAsync($"{{\"error\":\"unauthorized\",\"detail\":\"{failureMsg}\"}}");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(opt =>
            {
                opt.FallbackPolicy = opt.DefaultPolicy;
            });

            services.Configure<JwtOptions>(section);
            return services;
        }
    }
}
