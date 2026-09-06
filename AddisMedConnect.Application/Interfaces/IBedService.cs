using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.Interfaces;

public interface IBedService
{
    Task<IEnumerable<Bed>> GetAllBedsAsync();
    Task<IEnumerable<Bed>> GetBedsByHospitalIdAsync(Guid hospitalId);
    Task<IEnumerable<Bed>> GetAvailableBedsByHospitalIdAsync(Guid hospitalId);
    Task<Bed> CreateBedAsync(CreateBedDto dto);
    Task<bool> UpdateBedStatusAsync(Guid bedId, BedStatus status);
    Task<Bed?> UpdateBedAsync(Guid bedId, UpdateBedDto dto);
    Task<bool> DeleteBedAsync(Guid bedId);
}