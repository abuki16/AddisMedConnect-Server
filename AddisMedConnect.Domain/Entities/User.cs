using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public Guid? AssignedHospitalId { get; set; }
    public Hospital? AssignedHospital { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}