using AddisMedConnect.Application.DTOs;

namespace AddisMedConnect.Application.Interfaces;

public interface IHospitalService
{
    Task<IEnumerable<HospitalDto>> GetAllHospitalsAsync();
    Task<HospitalDto?> GetHospitalByIdAsync(Guid id);
    Task<IEnumerable<BedDto>> GetBedsByHospitalIdAsync(Guid hospitalId);
    Task<bool> UpdateBedStatusAsync(Guid bedId, int status);
}