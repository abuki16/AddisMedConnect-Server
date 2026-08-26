using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/beds")]
[Produces("application/json")]
public class BedsController : ControllerBase
{
    private readonly IBedService _bedService;

    public BedsController(IBedService bedService)
    {
        _bedService = bedService;
    }
     // POST: api/beds
[HttpPost]
[ProducesResponseType(typeof(Bed), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[EndpointSummary("Create a new bed for a specific hospital")]
public async Task<ActionResult<Bed>> CreateBed([FromBody] CreateBedDto dto)
{
    try
    {
        var createdBed = await _bedService.CreateBedAsync(dto);
        return CreatedAtAction(nameof(GetBedsByHospital), new { hospitalId = createdBed.HospitalId }, createdBed);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}
    // GET: api/beds
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Bed>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve all beds across all hospitals")]
    public async Task<ActionResult<IEnumerable<Bed>>> GetAllBeds()
    {
        var beds = await _bedService.GetAllBedsAsync();
        return Ok(beds);
    }

    // GET: api/beds/hospital/{hospitalId}
    [HttpGet("hospital/{hospitalId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<Bed>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieve all beds for a specific hospital")]
    public async Task<ActionResult<IEnumerable<Bed>>> GetBedsByHospital(Guid hospitalId)
    {
        var beds = await _bedService.GetBedsByHospitalIdAsync(hospitalId);
        return Ok(beds);
    }

    // GET: api/beds/hospital/{hospitalId}/available
    [HttpGet("hospital/{hospitalId:guid}/available")]
    [ProducesResponseType(typeof(IEnumerable<Bed>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve only available (unassigned) beds for a specific hospital")]
    public async Task<ActionResult<IEnumerable<Bed>>> GetAvailableBedsByHospital(Guid hospitalId)
    {
        var availableBeds = await _bedService.GetAvailableBedsByHospitalIdAsync(hospitalId);
        return Ok(availableBeds);
    }

    // PATCH: api/beds/{bedId}/status
    [HttpPatch("{bedId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [EndpointSummary("Directly update a bed's status and broadcast via SignalR")]
    public async Task<IActionResult> UpdateBedStatus(Guid bedId, [FromBody] UpdateBedStatusDto dto)
    {
        var success = await _bedService.UpdateBedStatusAsync(bedId, (BedStatus)dto.Status);
        if (!success) return NotFound();

        return NoContent();
    }
}

public record UpdateBedStatusDto(int Status);