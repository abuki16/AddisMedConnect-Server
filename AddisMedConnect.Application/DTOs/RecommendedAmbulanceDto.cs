namespace AddisMedConnect.Application.DTOs;

public record RecommendedAmbulanceDto(
    Guid Id,
    string PlateNumber,
    string? DriverName,
    string? PhoneNumber,
    double? CurrentLatitude,
    double? CurrentLongitude,
    bool IsAvailable,
    bool IsAtTargetHospital,
    string? StationedHospitalName,
    double? DistanceToHospitalKm,
    double? DistanceToPatientKm,
    int? EstimatedMinutesToPatient,
    string RecommendationBadge,
    string RecommendationReason,
    int PriorityRank
);
