using System.ComponentModel.DataAnnotations;

namespace AddisMedConnect.Application.DTOs;

public class CreateEmergencyCaseDto
{
    [Required(ErrorMessage = "Patient name is required.")]
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Incident reason is required.")]
    public string IncidentReason { get; set; } = string.Empty; 

    [Required(ErrorMessage = "Target hospital identifier is required.")]
    public string TargetHospitalId { get; set; } = string.Empty;

    public string? AssignedBedId { get; set; }
    public string? AssignedAmbulanceId { get; set; } 

    public string? PickupAddress { get; set; }
    public double? PickupLatitude { get; set; }
    public double? PickupLongitude { get; set; }
}