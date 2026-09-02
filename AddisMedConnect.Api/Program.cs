using AddisMedConnect.Api.Hubs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Infrastructure.Persistence;
using AddisMedConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
builder.Services.AddDbContext<AddisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CORS Configuration
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
    options.Events = new JwtBearerEvents { OnMessageReceived = context => { var token = context.Request.Query["access_token"]; if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs")) context.Token = token; return Task.CompletedTask; } };
});
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();

// 3. Register SignalR & Business Services
builder.Services.AddSignalR();
builder.Services.AddScoped<IBedNotificationService, BedNotificationService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IEmergencyService, EmergencyService>();
builder.Services.AddScoped<IBedService, BedService>();

// 4. Controllers, Reference Loop Handling, & Enum String Conversion
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var builderApp = builder.Build();

// 5. Database Initialization / Seeding
using (var scope = builderApp.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AddisDbContext>();
    await DbInitializer.SeedAsync(context);
}

if (builderApp.Environment.IsDevelopment())
{
    builderApp.MapOpenApi();
    builderApp.MapScalarApiReference(); // Adds Scalar UI at /scalar/v1
}

builderApp.UseExceptionHandler();
builderApp.UseStatusCodePages();

builderApp.UseHttpsRedirection();

// 6. Middleware Pipeline (Must include UseRouting before UseCors & MapEndpoints)
builderApp.UseRouting();

builderApp.UseCors("AllowClient");
builderApp.UseAuthentication();
builderApp.UseAuthorization();

// 7. Endpoint Mappings
builderApp.MapControllers();
builderApp.MapHub<BedHub>("/hubs/beds").RequireAuthorization().RequireCors("AllowClient");
builderApp.MapHub<EmergencyHub>("/hubs/emergency").RequireAuthorization().RequireCors("AllowClient");

builderApp.Run();
