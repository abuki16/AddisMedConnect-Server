using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("AuthLimiter")]
    [EndpointSummary("Authenticate user credentials and return a secure JWT access token")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);

        if (result.IsSuccess && result.Response is not null)
        {
            return Ok(result.Response);
        }

        if (result.IsLocked)
        {
            return StatusCode(StatusCodes.Status423Locked, new
            {
                message = result.ErrorMessage,
                isLocked = true,
                lockoutEnd = result.LockoutEnd,
                lockoutTier = result.LockoutTier,
                retryAfterSeconds = result.RetryAfterSeconds
            });
        }

        if (result.RemainingTrials.HasValue)
        {
            return Unauthorized(new { message = result.ErrorMessage });
        }

        return Unauthorized(new { message = result.ErrorMessage ?? "Invalid email/username or password." });
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Retrieve claims and profile identifiers for the currently authenticated user")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        role = User.FindFirstValue(ClaimTypes.Role),
        hospitalId = User.FindFirstValue("hospital_id"),
        ambulanceId = User.FindFirstValue("ambulance_id")
    });

    [HttpGet("test")]
    [Authorize]
    [EndpointSummary("Verify that the provided authentication token is active and valid")]
    public IActionResult Test() => Ok(new
    {
        message = "Authentication is working.",
        userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        role = User.FindFirstValue(ClaimTypes.Role)
    });

    [HttpGet("users")]
    [EndpointSummary("Retrieve a complete list of all registered system users")]
    public async Task<ActionResult<IEnumerable<UserManagementDto>>> GetUsers()
    {
        var users = await _authService.GetUsersAsync();
        return Ok(users);
    }

    [HttpPost("registerusers")]
    [EndpointSummary("Register a new system user account with first name, last name, role, and password confirmation")]
    public async Task<ActionResult<UserManagementDto>> RegisterUser(CreateUserDto dto)
    {
        var (success, errorMessage, user) = await _authService.RegisterUserAsync(dto);

        if (!success)
        {
            if (errorMessage == "A user already uses this email.")
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return CreatedAtAction(nameof(GetUsers), new { id = user!.Id }, user);
    }

    [HttpPut("users/{id:guid}")]
    [EndpointSummary("Update an existing user's first name, last name, profile information, role, hospital assignment, or password")]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(Guid id, UpdateUserDto dto)
    {
        var (success, errorMessage, notFound, user) = await _authService.UpdateUserAsync(id, dto);

        if (notFound) return NotFound();

        if (!success)
        {
            if (errorMessage == "A user already uses this email.")
                return Conflict(new { message = errorMessage });
            return BadRequest(new { message = errorMessage });
        }

        return Ok(user);
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [EndpointSummary("Permanently delete a user account from the system database")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserId = Guid.TryParse(currentUserIdStr, out var cid) ? cid : Guid.Empty;

        var (success, errorMessage, notFound) = await _authService.DeleteUserAsync(id, currentUserId);

        if (notFound) return NotFound();
        if (!success) return BadRequest(new { message = errorMessage });

        return NoContent();
    }

    [HttpPost("users/{id:guid}/unlock")]
    [EndpointSummary("Manually unlock a locked user account and reset failed login attempts")]
    public async Task<ActionResult<UserManagementDto>> UnlockUser(Guid id)
    {
        var user = await _authService.UnlockUserAsync(id);
        if (user is null) return NotFound(new { message = "User not found." });
        return Ok(user);
    }
}
