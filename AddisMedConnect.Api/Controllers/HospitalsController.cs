using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
    [EndpointSummary("Retrieve all registered hospitals with dynamic capacities")]
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

    [HttpPost]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(HospitalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Register a new partner hospital in the network")]
    public async Task<ActionResult<HospitalDto>> Create([FromBody] CreateHospitalDto dto)
    {
        try
        {
            var created = await _hospitalService.CreateHospitalAsync(dto);
            return CreatedAtAction(nameof(GetHospital), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(typeof(HospitalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Update details, code, contact or location of a partner hospital")]
    public async Task<ActionResult<HospitalDto>> Update(Guid id, [FromBody] UpdateHospitalDto dto)
    {
        try
        {
            var updated = await _hospitalService.UpdateHospitalAsync(id, dto);
            if (updated == null) return NotFound(new { message = $"Hospital with ID '{id}' was not found." });
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [EndpointSummary("Safely decommission and remove a hospital with zero active emergency cases")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _hospitalService.DeleteHospitalAsync(id);
            if (!deleted) return NotFound(new { message = $"Hospital with ID '{id}' was not found." });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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