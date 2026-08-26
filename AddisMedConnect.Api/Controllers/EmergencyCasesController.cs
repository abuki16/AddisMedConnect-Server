using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/emergency-cases")]
[Produces("application/json")]
public class EmergencyCasesController : ControllerBase
{
    private readonly IEmergencyService _emergencyService;

    public EmergencyCasesController(IEmergencyService emergencyService)
    {
        _emergencyService = emergencyService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EmergencyCaseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all emergency cases")]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetAll()
    {
        var cases = await _emergencyService.GetAllCasesAsync();
        return Ok(cases);
    }

    [HttpGet("hospital/{hospitalId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<EmergencyCaseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve active inbound (Dispatched) emergency cases for a specific hospital triage queue")]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetCasesByHospital(Guid hospitalId)
    {
        var cases = await _emergencyService.GetCasesByHospitalAsync(hospitalId);
        return Ok(cases);
    }

    [HttpGet("hospital/{hospitalId:guid}/active")]
    [ProducesResponseType(typeof(IEnumerable<EmergencyCaseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve active cases (Dispatched or Admitted) for discharge management")]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetActiveHospitalCases(Guid hospitalId)
    {
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
    [ProducesResponseType(typeof(EmergencyCaseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Create a new emergency case and set status strictly to Dispatched")]
    public async Task<ActionResult<EmergencyCaseDto>> Create([FromBody] CreateEmergencyCaseDto dto)
    {
        var created = await _emergencyService.CreateCaseAsync(dto);
        return CreatedAtAction(nameof(GetByIncidentNumber), new { incidentNumber = created.IncidentNumber }, created);
    }

    [HttpPatch("{incidentNumber}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update operational status of an emergency case (e.g., Resolve/Discharge)")]
    public async Task<IActionResult> UpdateStatus(string incidentNumber, [FromBody] UpdateCaseStatusDto dto)
    {
        var success = await _emergencyService.UpdateCaseStatusAsync(incidentNumber, dto.Status);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("{incidentNumber}/triage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Complete triage intake, transition status to Admitted, and occupy bed")]
    public async Task<IActionResult> CompleteTriage(string incidentNumber, [FromBody] TriageAssessmentDto dto)
    {
        var success = await _emergencyService.CompleteTriageAsync(incidentNumber, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("check-capacity")]
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

    [HttpGet("available-ambulances")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("Fetch all currently available ambulances")]
    public async Task<IActionResult> GetAvailableAmbulances()
    {
        var ambulances = await _emergencyService.GetAvailableAmbulancesAsync();
        return Ok(ambulances);
    }

    [HttpPost("{incidentNumber}/assign")]
    [ProducesResponseType(typeof(EmergencyCaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EndpointSummary("Assign a free bed and ambulance to an emergency incident")]
    public async Task<ActionResult<EmergencyCaseDto>> AssignResources(string incidentNumber, [FromBody] AssignResourcesDto dto)
    {
        try
        {
            var result = await _emergencyService.AssignResourcesAsync(incidentNumber, dto.BedId, dto.AmbulanceId);
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