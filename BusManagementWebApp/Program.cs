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

    // Allowed without authentication
    var publicPaths = new[]
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

    bool isPublic = publicPaths.Any(p => path == p) || publicPrefixes.Any(p => path.StartsWith(p));
    if (isPublic)
    {
        await next();
        return;
    }

    // check cookie token
    var token = context.Request.Cookies["AccessToken"];
    if (string.IsNullOrEmpty(token))
    {
        context.Response.Redirect(BuildLoginRedirectUrl());
        return;
    }

    try
    {
        var key = app.Configuration["JwtSettings:SecretKey"] ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            // if jwt not configured, allow for safety
            await next();
            return;
        }

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
        context.User = principal;
        await next();
    }
    catch (Exception ex)
    {
        context.Response.Redirect(BuildLoginRedirectUrl());
    }
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
