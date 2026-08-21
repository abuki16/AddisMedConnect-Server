namespace AddisMedConnect.Application.DTOs;

public record HospitalDto(
    Guid Id,
    string Name,
    string SubCity,
    string Address,
    double? Latitude,
    double? Longitude,
    string ContactPhone,
    int TotalBeds,
    int AvailableBeds
);

public record BedDto(
    Guid Id,
    string BedNumber,
    string WardType,
    string Status,
    Guid HospitalId,
    DateTime LastStatusUpdate
);

public record UpdateBedStatusDto(
    int Status
);