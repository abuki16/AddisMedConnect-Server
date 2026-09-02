using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<Ambulance>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all registered ambulances")]
    public async Task<ActionResult<IEnumerable<Ambulance>>> GetAll()
    {
        var ambulances = await _context.Ambulances.ToListAsync();
        return Ok(ambulances);
    }

    [HttpPost]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
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

    [HttpGet("mine")]
    [Authorize(Roles = "AmbulanceDriver")]
    public async Task<ActionResult<object>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == userId);
        if (ambulance is null) return NotFound(new { message = "No ambulance is linked to this driver account." });
        var assignment = await _context.EmergencyCases.Include(c => c.TargetHospital)
            .Where(c => c.AssignedAmbulanceId == ambulance.Id &&
                        (c.Status == Domain.Enums.CaseStatus.Dispatched ||
                         c.Status == Domain.Enums.CaseStatus.InTransit ||
                         c.Status == Domain.Enums.CaseStatus.ArrivedAtTriage))
            .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync();
        return Ok(new { ambulance, assignment });
    }

    [HttpPost("mine/location")]
    [Authorize(Roles = "AmbulanceDriver")]
    public async Task<IActionResult> UpdateMyLocation([FromBody] AddisMedConnect.Application.DTOs.UpdateAmbulanceLocationDto dto)
    {
        if (dto.Latitude is < -90 or > 90 || dto.Longitude is < -180 or > 180) return BadRequest(new { message = "Coordinates are outside valid ranges." });
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == userId);
        if (ambulance is null) return NotFound(new { message = "No ambulance is linked to this driver account." });
        ambulance.CurrentLatitude = dto.Latitude; ambulance.CurrentLongitude = dto.Longitude; ambulance.LastLocationUpdatedAt = DateTime.UtcNow;
        _context.AmbulanceLocations.Add(new Domain.Entities.AmbulanceLocation { AmbulanceId = ambulance.Id, Latitude = dto.Latitude, Longitude = dto.Longitude, AddressLabel = dto.AddressLabel, IncidentNumber = await _context.EmergencyCases.Where(c => c.AssignedAmbulanceId == ambulance.Id && c.Status != Domain.Enums.CaseStatus.Resolved && c.Status != Domain.Enums.CaseStatus.Cancelled).Select(c => c.IncidentNumber).FirstOrDefaultAsync() });
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{ambulanceId:guid}/locations")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    public async Task<IActionResult> GetLocations(Guid ambulanceId) => Ok(await _context.AmbulanceLocations.Where(l => l.AmbulanceId == ambulanceId).OrderByDescending(l => l.RecordedAt).Take(100).ToListAsync());

}

public record CreateAmbulanceDto(
    string PlateNumber,
    string? DriverName,
    string? PhoneNumber,
    double? CurrentLatitude,
    double? CurrentLongitude
);
