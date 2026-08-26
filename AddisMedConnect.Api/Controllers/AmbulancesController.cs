using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/ambulances")]
[Produces("application/json")]
public class AmbulancesController : ControllerBase
{
    private readonly AddisDbContext _context;

    public AmbulancesController(AddisDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Ambulance>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all registered ambulances")]
    public async Task<ActionResult<IEnumerable<Ambulance>>> GetAll()
    {
        var ambulances = await _context.Ambulances.ToListAsync();
        return Ok(ambulances);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Ambulance), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Register a new ambulance into the fleet")]
    public async Task<ActionResult<Ambulance>> Create([FromBody] CreateAmbulanceDto dto)
    {
        var existingAmbulance = await _context.Ambulances
            .AnyAsync(a => a.PlateNumber.ToLower() == dto.PlateNumber.ToLower());

        if (existingAmbulance)
        {
            return Conflict(new { message = $"An ambulance with plate number '{dto.PlateNumber}' is already registered." });
        }

        var ambulance = new Ambulance
        {
            Id = Guid.NewGuid(),
            PlateNumber = dto.PlateNumber,
            DriverName = dto.DriverName,
            PhoneNumber = dto.PhoneNumber,
            IsAvailable = true,
            CurrentLatitude = dto.CurrentLatitude,
            CurrentLongitude = dto.CurrentLongitude
        };

        _context.Ambulances.Add(ambulance);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = ambulance.Id }, ambulance);
    }
}

public record CreateAmbulanceDto(
    string PlateNumber, 
    string? DriverName, 
    string? PhoneNumber, 
    double? CurrentLatitude, 
    double? CurrentLongitude
);