using AddisMedConnect.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace AddisMedConnect.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Computed property combining first and last name (NotMapped so EF Core ignores it for database columns)
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public Guid? AssignedHospitalId { get; set; }
    public Hospital? AssignedHospital { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}