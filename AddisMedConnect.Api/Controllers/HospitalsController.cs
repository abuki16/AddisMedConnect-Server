using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HospitalsController : ControllerBase
{
    private readonly IHospitalService _hospitalService;

    public HospitalsController(IHospitalService hospitalService)
    {
        _hospitalService = hospitalService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<HospitalDto>>> GetHospitals()
    {
        var hospitals = await _hospitalService.GetAllHospitalsAsync();
        return Ok(hospitals);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HospitalDto>> GetHospital(Guid id)
    {
        var hospital = await _hospitalService.GetHospitalByIdAsync(id);
        if (hospital == null) return NotFound();
        return Ok(hospital);
    }

    [HttpGet("{id:guid}/beds")]
    public async Task<ActionResult<IEnumerable<BedDto>>> GetHospitalBeds(Guid id)
    {
        var beds = await _hospitalService.GetBedsByHospitalIdAsync(id);
        return Ok(beds);
    }

    [HttpPatch("beds/{bedId:guid}/status")]
    public async Task<IActionResult> UpdateBedStatus(Guid bedId, [FromBody] UpdateBedStatusDto dto)
    {
        var result = await _hospitalService.UpdateBedStatusAsync(bedId, dto.Status);
        if (!result) return NotFound();
        return NoContent();
    }
}