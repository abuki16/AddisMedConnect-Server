using AddisMedConnect.Domain;
namespace AddisMedConnect.Domain.Entities;

public class Ambulance
{
    public Guid Id { get; set; }
    public string PlateNumber { get; set; } = string.Empty; // Mandatory
    public string? DriverName { get; set; }                 // Optional
    public Guid? DriverUserId { get; set; }
    public User? DriverUser { get; set; }
    public string? PhoneNumber { get; set; }                // Optional
    public bool IsAvailable { get; set; } = true;

    // Location tracking for proximity dispatch
    public double? CurrentLatitude { get; set; }
    public double? CurrentLongitude { get; set; }
    public DateTime? LastLocationUpdatedAt { get; set; }
    public ICollection<AmbulanceLocation> LocationHistory { get; set; } = new List<AmbulanceLocation>();
}
