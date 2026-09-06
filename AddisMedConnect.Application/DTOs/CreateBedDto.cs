using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.DTOs;

public record CreateBedDto(
    string BedNumber,
    string WardType,
    string Code,
    Guid HospitalId
);

public record UpdateBedDto(
    string BedNumber,
    string WardType,
    string Code,
    Guid HospitalId,
    BedStatus Status
);