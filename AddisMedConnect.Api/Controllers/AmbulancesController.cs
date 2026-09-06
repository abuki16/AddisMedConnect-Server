using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Infrastructure.Persistence;
using AddisMedConnect.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/ambulances")]
[Produces("application/json")]
public class AmbulancesController : ControllerBase
{
    private readonly AddisDbContext _context;
    private readonly IHubContext<EmergencyHub> _emergencyHub;

    public AmbulancesController(AddisDbContext context, IHubContext<EmergencyHub> emergencyHub)
    {
        _context = context;
        _emergencyHub = emergencyHub;
    }

    [HttpGet]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<Ambulance>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all registered ambulances")]
    public async Task<ActionResult<IEnumerable<Ambulance>>> GetAll()
    {
        var ambulances = await _context.Ambulances
            .Include(a => a.DriverUser)
            .OrderBy(a => a.PlateNumber)
            .ToListAsync();
        return Ok(ambulances);
    }

    [HttpGet("{ambulanceId:guid}")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(Ambulance), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieve a single ambulance by ID")]
    public async Task<ActionResult<Ambulance>> GetById(Guid ambulanceId)
    {
        var ambulance = await _context.Ambulances
            .Include(a => a.DriverUser)
            .FirstOrDefaultAsync(a => a.Id == ambulanceId);
        if (ambulance is null) return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });
        return Ok(ambulance);
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
            .AnyAsync(a => a.PlateNumber.ToLower() == dto.PlateNumber.Trim().ToLower());

        if (existingAmbulance)
        {
            return Conflict(new { message = $"An ambulance with plate number '{dto.PlateNumber}' is already registered." });
        }

        var ambulance = new Ambulance
        {
            Id = Guid.NewGuid(),
            PlateNumber = dto.PlateNumber.Trim(),
            DriverName = string.IsNullOrWhiteSpace(dto.DriverName) ? null : dto.DriverName.Trim(),
            DriverUserId = dto.DriverUserId,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            IsAvailable = dto.IsAvailable,
            CurrentLatitude = dto.CurrentLatitude ?? 9.0300,
            CurrentLongitude = dto.CurrentLongitude ?? 38.7400,
            LastLocationUpdatedAt = DateTime.UtcNow
        };

        _context.Ambulances.Add(ambulance);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = ambulance.Id }, ambulance);
    }

    [HttpPut("{ambulanceId:guid}")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(Ambulance), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Update ambulance fleet vehicle details, plate number, driver, or availability")]
    public async Task<ActionResult<Ambulance>> Update(Guid ambulanceId, [FromBody] UpdateAmbulanceDto dto)
    {
        var ambulance = await _context.Ambulances.FindAsync(ambulanceId);
        if (ambulance is null) return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });

        var plateClash = await _context.Ambulances
            .AnyAsync(a => a.Id != ambulanceId && a.PlateNumber.ToLower() == dto.PlateNumber.Trim().ToLower());
        if (plateClash)
        {
            return Conflict(new { message = $"Another ambulance with plate number '{dto.PlateNumber}' already exists." });
        }

        ambulance.PlateNumber = dto.PlateNumber.Trim();
        ambulance.DriverName = string.IsNullOrWhiteSpace(dto.DriverName) ? null : dto.DriverName.Trim();
        ambulance.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        ambulance.DriverUserId = dto.DriverUserId;
        ambulance.IsAvailable = dto.IsAvailable;
        if (dto.CurrentLatitude is not null && dto.CurrentLongitude is not null)
        {
            ambulance.CurrentLatitude = dto.CurrentLatitude;
            ambulance.CurrentLongitude = dto.CurrentLongitude;
            ambulance.LastLocationUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(ambulance);
    }

    [HttpPatch("{ambulanceId:guid}/status")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Toggle or update an ambulance's operational status (available vs out-of-service)")]
    public async Task<IActionResult> UpdateStatus(Guid ambulanceId, [FromBody] UpdateAmbulanceStatusDto dto)
    {
        var ambulance = await _context.Ambulances.FindAsync(ambulanceId);
        if (ambulance is null) return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });

        ambulance.IsAvailable = dto.IsAvailable;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{ambulanceId:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Permanently decommission and delete an ambulance from the fleet")]
    public async Task<IActionResult> Delete(Guid ambulanceId)
    {
        var ambulance = await _context.Ambulances.FindAsync(ambulanceId);
        if (ambulance is null) return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });

        // Check if assigned to an active emergency case
        var hasActiveCase = await _context.EmergencyCases.AnyAsync(c =>
            c.AssignedAmbulanceId == ambulanceId &&
            c.Status != Domain.Enums.CaseStatus.Resolved &&
            c.Status != Domain.Enums.CaseStatus.Cancelled);

        if (hasActiveCase)
        {
            return Conflict(new { message = "Cannot delete ambulance while it is currently dispatched or on an active emergency mission." });
        }

        // For past resolved/cancelled cases, detach the ambulance reference
        var pastCases = await _context.EmergencyCases
            .Where(c => c.AssignedAmbulanceId == ambulanceId)
            .ToListAsync();
        foreach (var c in pastCases)
        {
            c.AssignedAmbulanceId = null;
        }

        // Delete associated location logs
        var locations = await _context.AmbulanceLocations
            .Where(l => l.AmbulanceId == ambulanceId)
            .ToListAsync();
        _context.AmbulanceLocations.RemoveRange(locations);

        _context.Ambulances.Remove(ambulance);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("mine")]
    [Authorize(Roles = "AmbulanceDriver")]
    public async Task<ActionResult<object>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == userId);
        if (ambulance is null) return NotFound(new { message = "No ambulance is linked to this driver account." });

        var assignment = await _context.EmergencyCases
            .Include(c => c.TargetHospital)
            .Include(c => c.AssignedBed)
            .Where(c => c.AssignedAmbulanceId == ambulance.Id &&
                        (c.Status == Domain.Enums.CaseStatus.Dispatched ||
                         c.Status == Domain.Enums.CaseStatus.InTransit ||
                         c.Status == Domain.Enums.CaseStatus.ArrivedAtTriage))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.IncidentNumber,
                c.CallerName,
                c.CallerPhone,
                c.PatientName,
                c.IncidentReason,
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                c.PickupAddress,
                c.PickupLatitude,
                c.PickupLongitude,
                c.CreatedAt,
                TargetHospital = new
                {
                    c.TargetHospital.Id,
                    c.TargetHospital.Name,
                    c.TargetHospital.Address,
                    c.TargetHospital.SubCity,
                    c.TargetHospital.Latitude,
                    c.TargetHospital.Longitude
                },
                AssignedBed = c.AssignedBed != null ? new
                {
                    c.AssignedBed.Id,
                    c.AssignedBed.BedNumber,
                    c.AssignedBed.WardType
                } : null,
                AssignedBedNumber = c.AssignedBed != null ? c.AssignedBed.BedNumber : null
            })
            .FirstOrDefaultAsync();

        return Ok(new { ambulance, assignment });
    }

    [HttpPost("mine/mission-status")]
    [Authorize(Roles = "AmbulanceDriver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update active ambulance mission status (InTransit or ArrivedAtTriage)")]
    public async Task<IActionResult> UpdateMissionStatus([FromBody] UpdateMissionStatusDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == userId);
        if (ambulance is null) return NotFound(new { message = "No ambulance is linked to this driver account." });

        var activeCase = await _context.EmergencyCases
            .Where(c => c.AssignedAmbulanceId == ambulance.Id &&
                        (c.Status == Domain.Enums.CaseStatus.Dispatched ||
                         c.Status == Domain.Enums.CaseStatus.InTransit ||
                         c.Status == Domain.Enums.CaseStatus.ArrivedAtTriage))
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeCase is null) return NotFound(new { message = "No active emergency mission assigned." });

        if (dto.Status == Domain.Enums.CaseStatus.InTransit || dto.Status == Domain.Enums.CaseStatus.ArrivedAtTriage)
        {
            activeCase.Status = dto.Status;
            await _context.SaveChangesAsync();

            await _emergencyHub.Clients.Group($"Hospital_{activeCase.TargetHospitalId}").SendAsync("QueueUpdated");
            await _emergencyHub.Clients.All.SendAsync("QueueUpdated");

            return Ok(new { message = $"Mission status updated to {dto.Status}", status = dto.Status.ToString() });
        }

        return BadRequest(new { message = "Ambulance driver can only transition status to InTransit or ArrivedAtTriage." });
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

public record UpdateMissionStatusDto(Domain.Enums.CaseStatus Status);

public record CreateAmbulanceDto(
    string PlateNumber,
    string? DriverName,
    string? PhoneNumber,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Guid? DriverUserId = null,
    bool IsAvailable = true
);

public record UpdateAmbulanceDto(
    string PlateNumber,
    string? DriverName,
    string? PhoneNumber,
    bool IsAvailable,
    Guid? DriverUserId = null,
    double? CurrentLatitude = null,
    double? CurrentLongitude = null
);

public record UpdateAmbulanceStatusDto(
    bool IsAvailable
);
