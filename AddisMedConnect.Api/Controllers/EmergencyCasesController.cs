using AddisMedConnect.Api.Hubs;
using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/emergency-cases")]
[Produces("application/json")]
[Authorize]
public class EmergencyCasesController : ControllerBase
{
    private readonly IEmergencyService _emergencyService;
    private readonly IHubContext<EmergencyHub> _emergencyHub;

    public EmergencyCasesController(IEmergencyService emergencyService, IHubContext<EmergencyHub> emergencyHub)
    {
        _emergencyService = emergencyService;
        _emergencyHub = emergencyHub;
    }

    [HttpGet]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetAll()
    {
        var cases = await _emergencyService.GetAllCasesAsync();
        return Ok(cases);
    }

    [HttpGet("hospital/{hospitalId:guid}")]
    [Authorize(Roles = "TriageNurse,DischargeClerk,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<EmergencyCaseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve active inbound (Dispatched) emergency cases for a specific hospital triage queue")]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetCasesByHospital(Guid hospitalId)
    {
        if (!CanAccessHospital(hospitalId)) return Forbid();
        var cases = await _emergencyService.GetCasesByHospitalAsync(hospitalId);
        return Ok(cases);
    }

    [HttpGet("hospital/{hospitalId:guid}/active")]
    [Authorize(Roles = "TriageNurse,DischargeClerk,SystemAdmin")]
    [ProducesResponseType(typeof(IEnumerable<EmergencyCaseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve active cases (Dispatched or Admitted) for discharge management")]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetActiveHospitalCases(Guid hospitalId)
    {
        if (!CanAccessHospital(hospitalId)) return Forbid();
        var cases = await _emergencyService.GetActiveHospitalCasesAsync(hospitalId);
        return Ok(cases);
    }

    [HttpGet("pending-triage-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve total count of cases awaiting triage nurse approval")]
    public async Task<IActionResult> GetPendingTriageCount()
    {
        var count = await _emergencyService.GetPendingTriageCountAsync();
        return Ok(new { count });
    }

    [HttpGet("hospital/{hospitalId:guid}/pending-triage-count")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve pending triage count for a specific hospital")]
    public async Task<IActionResult> GetPendingCountByHospital(Guid hospitalId)
    {
        var count = await _emergencyService.GetPendingTriageCountByHospitalAsync(hospitalId);
        return Ok(new { count });
    }

    [HttpGet("{incidentNumber}")]
    [ProducesResponseType(typeof(EmergencyCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieve an emergency case by incident number")]
    public async Task<ActionResult<EmergencyCaseDto>> GetByIncidentNumber(string incidentNumber)
    {
        var emergencyCase = await _emergencyService.GetCaseByIncidentNumberAsync(incidentNumber);
        if (emergencyCase == null) return NotFound();
        return Ok(emergencyCase);
    }

    [HttpPost]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(EmergencyCaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Create a new emergency case and set status strictly to Dispatched")]
    public async Task<ActionResult<EmergencyCaseDto>> Create([FromBody] CreateEmergencyCaseDto dto)
    {
        var created = await _emergencyService.CreateCaseAsync(dto);
        await _emergencyHub.Clients.Group($"Hospital_{created.TargetHospitalId}").SendAsync("ReceiveEmergencyDispatch", created);
        await _emergencyHub.Clients.All.SendAsync("QueueUpdated");
        return CreatedAtAction(nameof(GetByIncidentNumber), new { incidentNumber = created.IncidentNumber }, created);
    }

    [HttpPatch("{incidentNumber}/status")]
    [Authorize(Roles = "DischargeClerk,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update operational status of an emergency case (e.g., Resolve/Discharge)")]
    public async Task<IActionResult> UpdateStatus(string incidentNumber, [FromBody] UpdateCaseStatusDto dto)
    {
        var emergencyCase = await _emergencyService.GetCaseByIncidentNumberAsync(incidentNumber);
        if (emergencyCase is null) return NotFound();
        if (!CanAccessHospital(emergencyCase.TargetHospitalId)) return Forbid();
        var success = await _emergencyService.UpdateCaseStatusAsync(incidentNumber, dto.Status);
        if (!success) return NotFound();
        await _emergencyHub.Clients.Group($"Hospital_{emergencyCase.TargetHospitalId}").SendAsync("QueueUpdated");
        await _emergencyHub.Clients.All.SendAsync("QueueUpdated");
        return NoContent();
    }

    [HttpPost("{incidentNumber}/triage")]
    [Authorize(Roles = "TriageNurse,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Complete triage intake, transition status to Admitted, and occupy bed")]
    public async Task<IActionResult> CompleteTriage(string incidentNumber, [FromBody] TriageAssessmentDto dto)
    {
        var emergencyCase = await _emergencyService.GetCaseByIncidentNumberAsync(incidentNumber);
        if (emergencyCase is null) return NotFound();
        if (!CanAccessHospital(emergencyCase.TargetHospitalId)) return Forbid();
        try
        {
            var success = await _emergencyService.CompleteTriageAsync(incidentNumber, dto);
            if (!success) return Conflict(new { message = "Only dispatched cases can be admitted through triage." });
            await _emergencyHub.Clients.Group($"Hospital_{emergencyCase.TargetHospitalId}").SendAsync("QueueUpdated");
            await _emergencyHub.Clients.All.SendAsync("QueueUpdated");
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{incidentNumber}/discharge")]
    [Authorize(Roles = "DischargeClerk,SystemAdmin")]
    [EndpointSummary("Close a healed or transferred admission and free its occupied bed")]
    public async Task<IActionResult> Discharge(string incidentNumber)
    {
        var emergencyCase = await _emergencyService.GetCaseByIncidentNumberAsync(incidentNumber);
        if (emergencyCase is null) return NotFound();
        if (!CanAccessHospital(emergencyCase.TargetHospitalId)) return Forbid();
        var success = await _emergencyService.UpdateCaseStatusAsync(incidentNumber, CaseStatus.Resolved);
        if (!success) return NotFound();
        await _emergencyHub.Clients.Group($"Hospital_{emergencyCase.TargetHospitalId}").SendAsync("QueueUpdated");
        await _emergencyHub.Clients.All.SendAsync("QueueUpdated");
        return NoContent();
    }

    private bool CanAccessHospital(Guid hospitalId) => User.IsInRole("SystemAdmin") || User.FindFirstValue("hospital_id") == hospitalId.ToString();

    [HttpGet("check-capacity")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("Check hospital bed capacity and recommend nearest alternative if full")]
    public async Task<IActionResult> CheckCapacity(
        [FromQuery] Guid hospitalId,
        [FromQuery] string wardType,
        [FromQuery] double lat,
        [FromQuery] double lng)
    {
        var result = await _emergencyService.CheckCapacityAndFindAlternativeAsync(hospitalId, wardType, lat, lng);
        return Ok(result);
    }

    [HttpGet("hospital-recommendations")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [EndpointSummary("Rank nearby hospitals using caller coordinates and requested ward capacity")]
    public async Task<IActionResult> RecommendHospitals([FromQuery] string wardType, [FromQuery] double lat, [FromQuery] double lng)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180) return BadRequest(new { message = "Caller coordinates are invalid." });
        return Ok(await _emergencyService.FindRecommendedHospitalsAsync(wardType, lat, lng));
    }


    [HttpGet("available-ambulances")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("Fetch all currently available ambulances")]
    public async Task<IActionResult> GetAvailableAmbulances()
    {
        var ambulances = await _emergencyService.GetAvailableAmbulancesAsync();
        return Ok(ambulances);
    }

    [HttpPost("{incidentNumber}/assign")]
    [Authorize(Roles = "Dispatcher,SystemAdmin")]
    [ProducesResponseType(typeof(EmergencyCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Assign a free bed and ambulance to an emergency incident")]
    public async Task<ActionResult<EmergencyCaseDto>> AssignResources(string incidentNumber, [FromBody] AssignResourcesDto dto)
    {
        try
        {
            var result = await _emergencyService.AssignResourcesAsync(incidentNumber, dto.BedId, dto.AmbulanceId);
            await _emergencyHub.Clients.Group($"Hospital_{result.TargetHospitalId}").SendAsync("ReceiveEmergencyDispatch", result);
            await _emergencyHub.Clients.All.SendAsync("QueueUpdated");
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

public record UpdateCaseStatusDto(CaseStatus Status);
public record AssignResourcesDto(Guid BedId, Guid AmbulanceId);
