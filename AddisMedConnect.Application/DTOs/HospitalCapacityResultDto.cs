using AddisMedConnect.Domain.Entities;

namespace AddisMedConnect.Application.DTOs;

public class HospitalCapacityResultDto
{
    public bool IsAvailable { get; set; }
    public string Message { get; set; } = string.Empty;
    public Hospital? AlternativeHospital { get; set; }
    public double DistanceKm { get; set; }
    public int AvailableBedsCount { get; set; }
}