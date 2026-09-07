using AddisMedConnect.Api.Hubs;
using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/ambulances")]
[Produces("application/json")]
public class AmbulancesController : ControllerBase
{
    private readonly IAmbulanceService _ambulanceService;
    private readonly IHubContext<EmergencyHub> _emergencyHub;

    public AmbulancesController(IAmbulanceService ambulanceService, IHubContext<EmergencyHub> emergencyHub)
    {
        _ambulanceService = ambulanceService;
        _emergencyHub = emergencyHub;
    }

    [HttpGet]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<Ambulance>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all registered ambulances")]
    public async Task<ActionResult<IEnumerable<Ambulance>>> GetAll()
    {
        var ambulances = await _ambulanceService.GetAllAsync();
        return Ok(ambulances);
    }

    [HttpGet("{ambulanceId:guid}")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(Ambulance), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieve a single ambulance by ID")]
    public async Task<ActionResult<Ambulance>> GetById(Guid ambulanceId)
    {
        var ambulance = await _ambulanceService.GetByIdAsync(ambulanceId);
        if (ambulance is null)
            return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });
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
        try
        {
            var ambulance = await _ambulanceService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetAll), new { id = ambulance.Id }, ambulance);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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
        try
        {
            var ambulance = await _ambulanceService.UpdateAsync(ambulanceId, dto);
            if (ambulance is null)
                return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });
            return Ok(ambulance);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{ambulanceId:guid}/status")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Toggle or update an ambulance's operational status (available vs out-of-service)")]
    public async Task<IActionResult> UpdateStatus(Guid ambulanceId, [FromBody] UpdateAmbulanceStatusDto dto)
    {
        var updated = await _ambulanceService.UpdateStatusAsync(ambulanceId, dto.IsAvailable);
        if (!updated)
            return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });
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
        try
        {
            var deleted = await _ambulanceService.DeleteAsync(ambulanceId);
            if (!deleted)
                return NotFound(new { message = $"Ambulance with ID '{ambulanceId}' was not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = "AmbulanceDriver")]
    public async Task<ActionResult<object>> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ambulanceService.GetDriverAmbulanceAndAssignmentAsync(userId);
        if (result is null)
            return NotFound(new { message = "No ambulance is linked to this driver account." });
        return Ok(result);
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
        var (success, message, newStatus, hospitalId) = await _ambulanceService.UpdateMissionStatusAsync(userId, dto.Status);

        if (!success)
        {
            if (message.Contains("No ambulance") || message.Contains("No active"))
                return NotFound(new { message });
            return BadRequest(new { message });
        }

        if (hospitalId.HasValue)
        {
            await _emergencyHub.Clients.Group($"Hospital_{hospitalId.Value}").SendAsync("QueueUpdated");
        }
        await _emergencyHub.Clients.All.SendAsync("QueueUpdated");

        return Ok(new { message, status = newStatus?.ToString() });
    }

    [HttpPost("mine/location")]
    [Authorize(Roles = "AmbulanceDriver")]
    public async Task<IActionResult> UpdateMyLocation([FromBody] UpdateAmbulanceLocationDto dto)
    {
        if (dto.Latitude is < -90 or > 90 || dto.Longitude is < -180 or > 180)
            return BadRequest(new { message = "Coordinates are outside valid ranges." });

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var updated = await _ambulanceService.UpdateDriverLocationAsync(userId, dto);
        if (!updated)
            return NotFound(new { message = "No ambulance is linked to this driver account." });

        return NoContent();
    }

    [HttpGet("{ambulanceId:guid}/locations")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    public async Task<IActionResult> GetLocations(Guid ambulanceId)
    {
        var locations = await _ambulanceService.GetLocationsAsync(ambulanceId);
        return Ok(locations);
    }
}
