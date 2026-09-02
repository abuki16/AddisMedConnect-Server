namespace AddisMedConnect.Application.DTOs;

public record HospitalRecommendationDto(
    Guid HospitalId,
    string HospitalName,
    string SubCity,
    string Address,
    double DistanceKm,
    int AvailableBeds,
    bool HasRequestedWardCapacity,
    string Recommendation);
