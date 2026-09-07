using System;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.DTOs;

public record UpdateMissionStatusDto(CaseStatus Status);

public record CreateAmbulanceDto(
    string PlateNumber,
    string? DriverName,
    string? PhoneNumber,
    double? CurrentLatitude,
    double? CurrentLongitude,
    Guid? DriverUserId = null,
    bool IsAvailable = true
);

public record UpdateAmbulanceDto(
    string PlateNumber,
    string? DriverName,
    string? PhoneNumber,
    bool IsAvailable,
    Guid? DriverUserId = null,
    double? CurrentLatitude = null,
    double? CurrentLongitude = null
);

public record UpdateAmbulanceStatusDto(
    bool IsAvailable
);
