using AddisMedConnect.Api.Hubs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Infrastructure.Persistence;
using AddisMedConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
builder.Services.AddDbContext<AddisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Health Checks Configuration (Liveness & Readiness)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AddisDbContext>("database");

// 3. HybridCache Configuration (.NET 2-level caching: in-memory L1 + L2)
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
});

// 4. Rate Limiting Configuration (Enterprise protection against spam & brute force)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts))
            retryAfter = ((int)ts.TotalSeconds).ToString();

        context.HttpContext.Response.Headers.RetryAfter = retryAfter;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Title = "Rate limit exceeded",
            Detail = $"Too many requests. Please retry after {retryAfter} seconds.",
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://addismedconnect.et/errors/rate_limit_exceeded"
        }, ct);
    };

    // Dedicated Auth Limiter to prevent brute-force credential stuffing
    options.AddFixedWindowLimiter("AuthLimiter", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // General API token bucket limiter
    options.AddTokenBucketLimiter("ApiLimiter", opt =>
    {
        opt.TokenLimit = 60;
        opt.TokensPerPeriod = 30;
        opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        opt.QueueLimit = 5;
    });
});

// 5. CORS Configuration
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// 6. JWT Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

// 7. SignalR & Application Business Services Registration
builder.Services.AddSignalR();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAmbulanceService, AmbulanceService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBedNotificationService, BedNotificationService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IEmergencyService, EmergencyService>();
builder.Services.AddScoped<IBedService, BedService>();

// 8. Controllers, Reference Loop Handling, & Enum String Conversion
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var builderApp = builder.Build();

// 9. Database Initialization & Seeding
using (var scope = builderApp.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AddisDbContext>();
    await DbInitializer.SeedAsync(context);
}

if (builderApp.Environment.IsDevelopment())
{
    builderApp.MapOpenApi();
    builderApp.MapScalarApiReference(); // Scalar interactive API docs at /scalar/v1
}

builderApp.UseExceptionHandler();
builderApp.UseStatusCodePages();

if (!builderApp.Environment.IsDevelopment())
{
    builderApp.UseHttpsRedirection();
}

// 10. Security Headers Middleware
builderApp.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// 11. Middleware Pipeline
builderApp.UseRouting();
builderApp.UseCors("AllowClient");
builderApp.UseRateLimiter();
builderApp.UseAuthentication();
builderApp.UseAuthorization();

// 12. Client Static Files Hosting (Production SPA Support)
var clientDistPath = Path.GetFullPath(Path.Combine(builderApp.Environment.ContentRootPath, "..", "..", "AddisMedConnect-client", "dist", "AddisMedConnect-client", "browser"));
if (Directory.Exists(clientDistPath))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientDistPath);
    builderApp.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    builderApp.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
}

// 13. Health Checks Endpoints
builderApp.MapHealthChecks("/health/live").DisableRateLimiting();
builderApp.MapHealthChecks("/health/ready").DisableRateLimiting();

// 14. Endpoint Mappings & SignalR Hubs
builderApp.MapControllers();
builderApp.MapHub<BedHub>("/hubs/beds").RequireAuthorization().RequireCors("AllowClient").DisableRateLimiting();
builderApp.MapHub<EmergencyHub>("/hubs/emergency").RequireAuthorization().RequireCors("AllowClient").DisableRateLimiting();

if (Directory.Exists(clientDistPath))
{
    var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientDistPath);
    builderApp.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = fileProvider });
}

builderApp.Run();

public partial class Program { }
