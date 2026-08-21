using AddisMedConnect.Api.Hubs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Infrastructure.Persistence;
using AddisMedConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AddisDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Register SignalR & Services cleanly
builder.Services.AddSignalR();
builder.Services.AddScoped<IBedNotificationService, BedNotificationService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IEmergencyService, EmergencyService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AddisDbContext>();
    await DbInitializer.SeedAsync(context);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Adds Scalar UI at /scalar/v1
}

app.UseHttpsRedirection();
app.UseCors("AllowClient");
app.UseAuthorization();

app.MapControllers();
app.MapHub<BedHub>("/hubs/beds");

app.Run();