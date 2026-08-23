using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HospitalsController : ControllerBase
{
    private readonly IHospitalService _hospitalService;

    public HospitalsController(IHospitalService hospitalService)
    {
        _hospitalService = hospitalService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HospitalDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all registered hospitals")]
    public async Task<ActionResult<IEnumerable<HospitalDto>>> GetHospitals()
    {
        var hospitals = await _hospitalService.GetAllHospitalsAsync();
        return Ok(hospitals);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HospitalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieve a hospital by ID")]
    public async Task<ActionResult<HospitalDto>> GetHospital(Guid id)
    {
        var hospital = await _hospitalService.GetHospitalByIdAsync(id);
        if (hospital == null) return NotFound();
        return Ok(hospital);
    }

    [HttpGet("{id:guid}/beds")]
    [ProducesResponseType(typeof(IEnumerable<BedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieve live bed inventory for a specific hospital")]
    public async Task<ActionResult<IEnumerable<BedDto>>> GetHospitalBeds(Guid id)
    {
        var beds = await _hospitalService.GetBedsByHospitalIdAsync(id);
        return Ok(beds);
    }

    [HttpPatch("beds/{bedId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Update operational status of a specific hospital bed")]
    public async Task<IActionResult> UpdateBedStatus(Guid bedId, [FromBody] UpdateBedStatusDto dto)
    {
        var result = await _hospitalService.UpdateBedStatusAsync(bedId, dto.Status);
        if (!result) return NotFound();
        return NoContent();
    }
}