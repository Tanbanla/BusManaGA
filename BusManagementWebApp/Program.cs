using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Configuration;
using BusManagement.Data.Configs.DependencyInjection;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using BusManagement.Service.Configs.AutoMapper;
using BusManagement.Service.Configs.DependencyInjection;
using BusManagementWebApp.Configs;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.File(
        path: "logs/log.txt",
        fileSizeLimitBytes: 200 * 1024,
        rollOnFileSizeLimit: true,
        retainedFileCountLimit: 20,
        outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDependencyInjectionServices();
builder.Services.AddDependencyInjectionRepos();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<ConnectionStringOptions>(builder.Configuration.GetSection("ConnectionStrings"));

// JWT auth + default authorization (FallbackPolicy) defined in extension
//builder.Services.AddJwtAuth(builder.Configuration);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
    options.SerializerOptions.DictionaryKeyPolicy = null;
});

// current user provider (scoped) and http context accessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BusManagement.Services.UserContext.ICurrentUserProvider, BusManagement.Services.UserContext.CurrentUserProvider>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddDbContext<BusManagementDBContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BusManagementConnection")));

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Cached public menu paths (menus without role assignments are considered public)
List<string>? menuPublicPaths = null;

// Authentication middleware: require valid AccessToken for all requests except dashboard and public assets
app.Use(async (context, next) =>
{
    var path = (context.Request.Path.Value ?? string.Empty).ToLowerInvariant();

    string BuildLoginRedirectUrl()
    {
        var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/")
        {
            return "/Account/Login";
        }

        return $"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    // Allowed without authentication (built-in)
    var builtInPublicPaths = new[]
    {
        "/",
        "/home",
        "/home/index",
        "/account/login",
        "/account/logout",
        "/favicon.ico"
    };

    // static content prefixes
    var publicPrefixes = new[] { "/lib/", "/css/", "/js/", "/images/", "/img/", "/_content/" };

    // Build menu-based public paths once and cache
    if (menuPublicPaths == null)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var menuSvc = scope.ServiceProvider.GetService(typeof(BusManagement.Service.Services.Interfaces.ITmMenuService)) as BusManagement.Service.Services.Interfaces.ITmMenuService;
            var menuRoleSvc = scope.ServiceProvider.GetService(typeof(BusManagement.Service.Services.Interfaces.ITmMenuRoleService)) as BusManagement.Service.Services.Interfaces.ITmMenuRoleService;
            if (menuSvc != null && menuRoleSvc != null)
            {
                var menusResp = await menuSvc.SearchByConditionAsync(new BusManagement.Model.Common.SearchOptions
                {
                    PageNumber = 1,
                    PageSize = 100000,
                    Filters = new List<BusManagement.Model.Common.FilterOption>
                    {
                        new() { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true }
                    }
                });

                var menus = menusResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();
                var menuRolesResp = await menuRoleSvc.SearchByConditionAsync(new BusManagement.Model.Common.SearchOptions { PageNumber = -1, PageSize = 100000 });
                var menuRoles = menuRolesResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();

                var roleMap = menuRoles
                    .Where(x => x.INT_ROLE_ID.HasValue)
                    .GroupBy(x => x.INT_MENU_ID)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.INT_ROLE_ID!.Value).Distinct().ToHashSet());

                var list = new List<string>();
                foreach (var m in menus)
                {
                    if (m == null) continue;
                    if (roleMap.TryGetValue(m.ID, out var roles) && roles.Count > 0) continue; // menu has roles => not public

                    var controller = (m.CHR_CONTROLLER ?? string.Empty).Trim().ToLowerInvariant();
                    var action = (m.CHR_ACTION ?? "Index").Trim().ToLowerInvariant();
                    if (!string.IsNullOrWhiteSpace(controller))
                    {
                        var basePath = "/" + controller;
                        list.Add(basePath);
                        // add controller/action
                        list.Add(basePath + "/" + action);
                        // also add explicit index variant
                        if (!action.Equals("index", StringComparison.OrdinalIgnoreCase))
                        {
                            list.Add(basePath + "/index");
                        }
                    }
                }

                menuPublicPaths = list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            else
            {
                menuPublicPaths = new List<string>();
            }
        }
        catch
        {
            menuPublicPaths = new List<string>();
        }
    }

    bool isPublic = builtInPublicPaths.Any(p => path == p)
                || (menuPublicPaths?.Any(p => path == p) ?? false)
                || publicPrefixes.Any(p => path.StartsWith(p));

    var token = context.Request.Cookies["AccessToken"];

    // Nếu có token, luôn cố validate để set context.User (kể cả trang public)
    if (!string.IsNullOrEmpty(token))
    {
        try
        {
            var key = app.Configuration["JwtSettings:SecretKey"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(key))
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero,
                    ValidateLifetime = true
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                if (principal?.Identity is ClaimsIdentity ci)
                {
                    string? adid = principal.Claims.FirstOrDefault(c => string.Equals(c.Type, "adid", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "upn", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "preferred_username", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "unique_name", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "name", StringComparison.OrdinalIgnoreCase))?.Value;

                    if (!string.IsNullOrWhiteSpace(adid) && !ci.HasClaim(c => string.Equals(c.Type, "adid", StringComparison.OrdinalIgnoreCase)))
                    {
                        ci.AddClaim(new Claim("adid", adid));
                    }

                    string? emp = principal.Claims.FirstOrDefault(c => string.Equals(c.Type, "employee_code", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "emp", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(c.Type, "employee", StringComparison.OrdinalIgnoreCase))?.Value;

                    if (!string.IsNullOrWhiteSpace(emp) && !ci.HasClaim(c => string.Equals(c.Type, "employee_code", StringComparison.OrdinalIgnoreCase)))
                    {
                        ci.AddClaim(new Claim("employee_code", emp));
                    }
                }

                context.User = principal; // set User bất kể public hay không, miễn token hợp lệ
            }
        }
        catch
        {
            // token không hợp lệ -> coi như chưa đăng nhập, không set context.User
        }
    }

    // Trang public: luôn cho qua, dù có set được User hay không
    if (isPublic)
    {
        await next();
        return;
    }

    // Trang không public: bắt buộc phải có User đã authenticate
    if (context.User?.Identity?.IsAuthenticated != true)
    {
        context.Response.Redirect(BuildLoginRedirectUrl());
        return;
    }

    await next();
});

app.UseRouting();
app.UseCors("AllowAll");

//app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Account}/{action=Login}/{id?}");
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply EF Core migrations on startup when enabled in configuration or in Development
try
{
    var applyMigrations = builder.Configuration.GetValue<bool>("ApplyMigrationsOnStartup");
    if (applyMigrations || app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<BusManagementDBContext>();
            logger.LogInformation("Applying pending EF Core migrations...");
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            // Log and rethrow to prevent starting with invalid schema unless explicitly configured otherwise
            logger.LogError(ex, "An error occurred while applying database migrations.");
            throw;
        }
    }
}
catch (Exception ex)
{
    // If migration check fails early, log to console but allow app to continue startup fallback
    Console.WriteLine($"Migration check failed: {ex.Message}");
    throw;
}

// Seed role data and default admin account
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<BusManagementDBContext>();
        if (db.Database.CanConnect())
        {
            var now = DateTime.Now;
            var seedUser = "system";

            var roleNames = new[] { "User", "GA", "Admin" };
            foreach (var roleName in roleNames)
            {
                var roleExists = await db.TM_ROLEs.AnyAsync(r => r.NVCHR_ROLE_NAME == roleName);
                if (!roleExists)
                {
                    db.TM_ROLEs.Add(new TM_ROLE
                    {
                        NVCHR_ROLE_NAME = roleName,
                        NVCHR_DESCRIPTION = $"{roleName} role",
                        BIT_IS_ACTIVE = true,
                        CHR_USER_CREATE = seedUser,
                        DTM_CREATE = now
                    });
                }
            }

            await db.SaveChangesAsync();

            var adminRoleId = await db.TM_ROLEs
                .Where(r => r.NVCHR_ROLE_NAME == "Admin")
                .Select(r => r.ID)
                .FirstOrDefaultAsync();

            var adminExists = await db.TM_USERs.AnyAsync(u => u.CHR_ADID == "admin");
            if (!adminExists)
            {
                db.TM_USERs.Add(new TM_USER
                {
                    CHR_EMPLOYEE_ID = "admin",
                    CHR_ADID = "admin",
                    NVCHR_NAME = "Administrator",
                    CHR_PASSWORD = "admin",
                    CHR_POSITION = "Admin",
                    INT_ROLE_ID = adminRoleId > 0 ? adminRoleId : null,
                    BIT_IS_ACTIVE = true,
                    DTM_LAST_LOGIN = null,
                    CHR_USER_CREATE = seedUser,
                    DTM_CREATE = now
                });

                await db.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
        logger?.LogWarning(ex, "Runtime seeding of roles/admin user failed");
    }
}

app.Run();
