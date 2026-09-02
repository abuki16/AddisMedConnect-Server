using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AddisMedConnect.Domain.Enums;
using Microsoft.AspNetCore.Authorization;

namespace AddisMedConnect.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AddisDbContext context, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [EndpointSummary("Authenticate user credentials and return a secure JWT access token")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var user = await context.Users.Include(u => u.AssignedHospital)
            .SingleOrDefaultAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower());
        if (user is null || new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        var ambulance = await context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == user.Id);
        var expires = DateTime.UtcNow.AddHours(8);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id.ToString()), new(ClaimTypes.Email, user.Email), new(ClaimTypes.Role, user.Role.ToString()) };
        if (user.AssignedHospitalId is not null) claims.Add(new("hospital_id", user.AssignedHospitalId.ToString()!));
        if (ambulance is not null) claims.Add(new("ambulance_id", ambulance.Id.ToString()));
        var token = new JwtSecurityToken(claims: claims, expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponseDto(new JwtSecurityTokenHandler().WriteToken(token), expires,
            new AuthenticatedUserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.AssignedHospitalId, user.AssignedHospital?.Name, ambulance?.Id, ambulance?.PlateNumber)));
    }

    [HttpGet("me")]
    [Authorize]
    [EndpointSummary("Retrieve claims and profile identifiers for the currently authenticated user")]
    public IActionResult Me() => Ok(new { id = User.FindFirstValue(ClaimTypes.NameIdentifier), role = User.FindFirstValue(ClaimTypes.Role), hospitalId = User.FindFirstValue("hospital_id"), ambulanceId = User.FindFirstValue("ambulance_id") });

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
    [Authorize(Roles = "SystemAdmin")]
    [EndpointSummary("Retrieve a complete list of all registered system users")]
    public async Task<ActionResult<IEnumerable<UserManagementDto>>> GetUsers() => Ok(await UserList().ToListAsync());

    [HttpPost("registerusers")]
    [Authorize(Roles = "SystemAdmin")]
    [EndpointSummary("Register a new system user account with first name, last name, role, and password confirmation")]
    public async Task<ActionResult<UserManagementDto>> RegisterUser(CreateUserDto dto)
    {
        if (dto.Password != dto.ConfirmPassword) return BadRequest(new { message = "Password and confirmation must match." });
        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role)) return BadRequest(new { message = "The selected role is invalid." });
        if (!IsStrongPassword(dto.Password)) return BadRequest(new { message = PasswordRequirementMessage });
        if (await context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.Trim().ToLower())) return Conflict(new { message = "A user already uses this email." });
        if (dto.HospitalId is not null && !await context.Hospitals.AnyAsync(h => h.Id == dto.HospitalId)) return BadRequest(new { message = "Assigned hospital was not found." });

        var user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            Role = role,
            AssignedHospitalId = dto.HospitalId
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, dto.Password);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, await UserList().SingleAsync(u => u.Id == user.Id));
    }

    [HttpPut("users/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [EndpointSummary("Update an existing user's first name, last name, profile information, role, hospital assignment, or password")]
    public async Task<ActionResult<UserManagementDto>> UpdateUser(Guid id, UpdateUserDto dto)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (!Enum.TryParse<UserRole>(dto.Role, true, out var role)) return BadRequest(new { message = "The selected role is invalid." });
        if (await context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == dto.Email.Trim().ToLower())) return Conflict(new { message = "A user already uses this email." });
        if (dto.HospitalId is not null && !await context.Hospitals.AnyAsync(h => h.Id == dto.HospitalId)) return BadRequest(new { message = "Assigned hospital was not found." });

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = dto.Email.Trim().ToLower();
        user.PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty;
        user.Role = role;
        user.AssignedHospitalId = dto.HospitalId;

        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            if (!IsStrongPassword(dto.NewPassword)) return BadRequest(new { message = PasswordRequirementMessage });
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, dto.NewPassword);
        }

        await context.SaveChangesAsync();
        return Ok(await UserList().SingleAsync(u => u.Id == id));
    }

    [HttpDelete("users/{id:guid}")]
    [Authorize(Roles = "SystemAdmin")]
    [EndpointSummary("Permanently delete a user account from the system database")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        if (User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString()) return BadRequest(new { message = "You cannot delete your own administrator account." });
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();

        context.Users.Remove(user);
        await context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<UserManagementDto> UserList() => context.Users
        .Include(u => u.AssignedHospital)
        .OrderBy(u => u.FirstName)
        .ThenBy(u => u.LastName)
        .Select(u => new UserManagementDto(
            u.Id,
            u.FullName,
            u.Email,
            u.PhoneNumber,
            u.Role.ToString(),
            u.AssignedHospitalId,
            u.AssignedHospital != null ? u.AssignedHospital.Name : null,
            u.CreatedAt
        ));

    private const string PasswordRequirementMessage = "Password must be at least 12 characters and include an uppercase letter, lowercase letter, number, and special character.";

    private static bool IsStrongPassword(string password) =>
        password.Length >= 12 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character));
}
