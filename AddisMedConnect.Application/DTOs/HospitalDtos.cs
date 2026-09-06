using System.ComponentModel.DataAnnotations;

namespace AddisMedConnect.Application.DTOs;

public record HospitalDto(
    Guid Id,
    string Code,
    string Name,
    string SubCity,
    string Address,
    double? Latitude,
    double? Longitude,
    string ContactPhone,
    int TotalBeds,
    int AvailableBeds
);

public class CreateHospitalDto
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string SubCity { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Required, Phone]
    public string ContactPhone { get; set; } = string.Empty;
}

public class UpdateHospitalDto
{
    [Required, StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string SubCity { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Required, Phone]
    public string ContactPhone { get; set; } = string.Empty;
}

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