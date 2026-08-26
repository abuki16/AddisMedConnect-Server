using AddisMedConnect.Api.Hubs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Infrastructure.Persistence;
using AddisMedConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
builder.Services.AddDbContext<AddisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. CORS Configuration (Allows credentials for headers/cookies)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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

builderApp.UseHttpsRedirection();

// 6. Middleware Pipeline (Must include UseRouting before UseCors & MapEndpoints)
builderApp.UseRouting();

builderApp.UseCors("AllowClient");

builderApp.UseAuthorization();

// 7. Endpoint Mappings
builderApp.MapControllers();
builderApp.MapHub<BedHub>("/hubs/beds").RequireCors("AllowClient");
builderApp.MapHub<EmergencyHub>("/hubs/emergency").RequireCors("AllowClient");

builderApp.Run();