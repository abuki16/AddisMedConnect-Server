using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.Interfaces;

public interface IEmergencyService
{
    Task<IEnumerable<EmergencyCaseDto>> GetAllCasesAsync();
    Task<EmergencyCaseDto?> GetCaseByIdAsync(Guid id);
    Task<EmergencyCaseDto> CreateCaseAsync(CreateEmergencyCaseDto dto);
    Task<bool> UpdateCaseStatusAsync(Guid id, CaseStatus status);
}

public record EmergencyCaseDto(
    Guid Id,
    string IncidentNumber,
    string PatientName,
    string ChiefComplaint,
    CaseStatus Status,
    TriagePriority Priority,
    Guid TargetHospitalId,
    string TargetHospitalName,
    Guid? AssignedBedId,
    string? BedNumber,
    string? PickupAddress,
    DateTime CreatedAt
);

public record CreateEmergencyCaseDto(
    string PatientName,
    string ChiefComplaint,
    TriagePriority Priority,
    Guid TargetHospitalId,
    Guid? AssignedBedId,
    string? PickupAddress,
    double? PickupLatitude,
    double? PickupLongitude
);