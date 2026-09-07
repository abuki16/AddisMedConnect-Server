using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.Interfaces;

public interface IAmbulanceService
{
    Task<IEnumerable<Ambulance>> GetAllAsync();
    Task<Ambulance?> GetByIdAsync(Guid ambulanceId);
    Task<Ambulance> CreateAsync(CreateAmbulanceDto dto);
    Task<Ambulance?> UpdateAsync(Guid ambulanceId, UpdateAmbulanceDto dto);
    Task<bool> UpdateStatusAsync(Guid ambulanceId, bool isAvailable);
    Task<bool> DeleteAsync(Guid ambulanceId);
    Task<object?> GetDriverAmbulanceAndAssignmentAsync(Guid driverUserId);
    Task<(bool Success, string Message, CaseStatus? NewStatus, Guid? HospitalId)> UpdateMissionStatusAsync(Guid driverUserId, CaseStatus status);
    Task<bool> UpdateDriverLocationAsync(Guid driverUserId, UpdateAmbulanceLocationDto dto);
    Task<IEnumerable<AmbulanceLocation>> GetLocationsAsync(Guid ambulanceId);
}
