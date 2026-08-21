using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Domain.Entities;

public class Bed
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BedNumber { get; set; } = string.Empty;
    public string WardType { get; set; } = string.Empty;
    public BedStatus Status { get; set; } = BedStatus.Available;
    
    public Guid HospitalId { get; set; }
    public Hospital Hospital { get; set; } = null!;

    public Guid? CurrentCaseId { get; set; }
    public EmergencyCase? CurrentCase { get; set; }

    public DateTime LastStatusUpdate { get; set; } = DateTime.UtcNow;
}