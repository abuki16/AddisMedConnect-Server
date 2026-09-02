using System.ComponentModel.DataAnnotations;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Domain.Entities;

public class EmergencyCase
{
    [Key]
    public string IncidentNumber { get; set; } = string.Empty; // Acts as the primary key

    // Caller Information
    public string CallerName { get; set; } = string.Empty;
    public string CallerPhone { get; set; } = string.Empty;

    public string PatientName { get; set; } = string.Empty;
    public string IncidentReason { get; set; } = string.Empty;
    public CaseStatus Status { get; set; } = CaseStatus.PendingDispatch;
    public TriagePriority Priority { get; set; } = TriagePriority.Yellow;

    public string? PickupAddress { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }

    public Guid? AssignedAmbulanceId { get; set; }
    public Ambulance? AssignedAmbulance { get; set; }

    public Guid TargetHospitalId { get; set; }
    public Hospital TargetHospital { get; set; } = null!;

    public Guid? AssignedBedId { get; set; }
    public Bed? AssignedBed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}