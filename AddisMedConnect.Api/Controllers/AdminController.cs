using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
[Authorize(Roles = "SystemAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(AdminMetricsDto), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieve pre-aggregated metropolitan emergency metrics for central command")]
    public async Task<ActionResult<AdminMetricsDto>> GetMetrics()
    {
        var metrics = await _adminService.GetMetricsAsync();
        return Ok(metrics);
    }
}
