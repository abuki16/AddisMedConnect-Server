namespace AddisMedConnect.Application.DTOs;

public record HospitalAvailabilityDto(
    Guid HospitalId,
    string HospitalName,
    string SubCity,
    List<BedDto> AvailableBeds
);