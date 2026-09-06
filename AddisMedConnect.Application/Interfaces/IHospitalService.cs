using AddisMedConnect.Application.DTOs;

namespace AddisMedConnect.Application.Interfaces;

public interface IHospitalService
{
    Task<IEnumerable<HospitalDto>> GetAllHospitalsAsync();
    Task<HospitalDto?> GetHospitalByIdAsync(Guid id);
    Task<HospitalDto> CreateHospitalAsync(CreateHospitalDto dto);
    Task<HospitalDto?> UpdateHospitalAsync(Guid id, UpdateHospitalDto dto);
    Task<bool> DeleteHospitalAsync(Guid id);
    Task<IEnumerable<BedDto>> GetBedsByHospitalIdAsync(Guid hospitalId);
    Task<IEnumerable<HospitalAvailabilityDto>> GetAvailableBedsAsync();
    Task<bool> UpdateBedStatusAsync(Guid bedId, int status);
}