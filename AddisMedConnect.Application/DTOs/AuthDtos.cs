using System.ComponentModel.DataAnnotations;

namespace AddisMedConnect.Application.DTOs;

public record LoginDto(string Email, string Password);
public record AuthenticatedUserDto(Guid Id, string FullName, string Email, string Role, Guid? HospitalId, string? HospitalName, Guid? AmbulanceId, string? PlateNumber);
public record LoginResponseDto(string AccessToken, DateTime ExpiresAt, AuthenticatedUserDto User);
public record UpdateAmbulanceLocationDto(double Latitude, double Longitude, string? AddressLabel);
public record UserManagementDto(Guid Id, string FullName, string Email, string PhoneNumber, string Role, Guid? HospitalId, string? HospitalName, DateTime CreatedAt);

public class CreateUserDto
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(12)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public Guid? HospitalId { get; set; }
}

public class UpdateUserDto
{
    [Required, StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    public Guid? HospitalId { get; set; }

    public string? NewPassword { get; set; }
}
