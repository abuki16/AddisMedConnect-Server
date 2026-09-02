namespace AddisMedConnect.Domain.Entities;

/// <summary>Immutable telemetry point emitted by an ambulance or its assigned driver.</summary>
public class AmbulanceLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AmbulanceId { get; set; }
    public Ambulance Ambulance { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? AddressLabel { get; set; }
    public string? IncidentNumber { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
