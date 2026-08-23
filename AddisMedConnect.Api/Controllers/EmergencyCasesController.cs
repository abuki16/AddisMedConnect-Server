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
    [EndpointSummary("Create a new emergency case and optionally reserve a bed")]
    public async Task<ActionResult<EmergencyCaseDto>> Create([FromBody] CreateEmergencyCaseDto dto)
    {
        var created = await _emergencyService.CreateCaseAsync(dto);
        return CreatedAtAction(nameof(GetByIncidentNumber), new { incidentNumber = created.IncidentNumber }, created);
    }

    [HttpPatch("{incidentNumber}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update operational status of an emergency case (auto-releases bed if cancelled/resolved)")]
    public async Task<IActionResult> UpdateStatus(string incidentNumber, [FromBody] UpdateCaseStatusDto dto)
    {
        var success = await _emergencyService.UpdateCaseStatusAsync(incidentNumber, dto.Status);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPost("{incidentNumber}/triage")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Complete triage intake upon patient arrival")]
    public async Task<IActionResult> CompleteTriage(string incidentNumber, [FromBody] TriageAssessmentDto dto)
    {
        var success = await _emergencyService.CompleteTriageAsync(incidentNumber, dto);
        if (!success) return NotFound();
        return NoContent();
    }
}

public record UpdateCaseStatusDto(CaseStatus Status);