using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.DTOs;

public record EmergencyCaseDto(
    string IncidentNumber,
    string CallerName,
    string CallerPhone,
    string PatientName,
    string IncidentReason,
    CaseStatus Status,
    TriagePriority Priority,
    Guid TargetHospitalId,
    string TargetHospitalName,
    Guid? AssignedBedId,
    string? BedNumber,
    Guid? AssignedAmbulanceId,
    string? AmbulancePlateNumber,
    string? PickupAddress,
    DateTime CreatedAt,
    double? PickupLatitude = null,
    double? PickupLongitude = null
);