using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/emergency-cases")]
public class EmergencyCasesController : ControllerBase
{
    private readonly IEmergencyService _emergencyService;

    public EmergencyCasesController(IEmergencyService emergencyService)
    {
        _emergencyService = emergencyService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmergencyCaseDto>>> GetAll()
    {
        var cases = await _emergencyService.GetAllCasesAsync();
        return Ok(cases);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EmergencyCaseDto>> GetById(Guid id)
    {
        var emergencyCase = await _emergencyService.GetCaseByIdAsync(id);
        if (emergencyCase == null) return NotFound();
        return Ok(emergencyCase);
    }

    [HttpPost]
    public async Task<ActionResult<EmergencyCaseDto>> Create([FromBody] CreateEmergencyCaseDto dto)
    {
        var created = await _emergencyService.CreateCaseAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCaseStatusDto dto)
    {
        var success = await _emergencyService.UpdateCaseStatusAsync(id, dto.Status);
        if (!success) return NotFound();
        return NoContent();
    }
}

public record UpdateCaseStatusDto(CaseStatus Status);