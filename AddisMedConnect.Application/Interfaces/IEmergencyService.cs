using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.Interfaces;

public interface IEmergencyService
{
    Task<IEnumerable<EmergencyCaseDto>> GetAllCasesAsync();
    Task<IEnumerable<EmergencyCaseDto>> GetCasesByHospitalAsync(Guid hospitalId); // Fetches Dispatched cases for Triage Nurse
    Task<IEnumerable<EmergencyCaseDto>> GetActiveHospitalCasesAsync(Guid hospitalId); // Fetches Dispatched/Admitted cases for Discharge Clerk
    Task<EmergencyCaseDto?> GetCaseByIncidentNumberAsync(string incidentNumber);
    Task<EmergencyCaseDto> CreateCaseAsync(CreateEmergencyCaseDto dto);
    Task<bool> UpdateCaseStatusAsync(string incidentNumber, CaseStatus status);
    Task<bool> CompleteTriageAsync(string incidentNumber, TriageAssessmentDto dto);
    
    Task<int> GetPendingTriageCountAsync();
    Task<int> GetPendingTriageCountByHospitalAsync(Guid hospitalId);
    
    Task<HospitalCapacityResultDto> CheckCapacityAndFindAlternativeAsync(Guid hospitalId, string wardType, double currentLat, double currentLng);
    Task<IEnumerable<HospitalRecommendationDto>> FindRecommendedHospitalsAsync(string wardType, double latitude, double longitude);
    Task<List<Ambulance>> GetAvailableAmbulancesAsync();
    Task<EmergencyCaseDto> AssignResourcesAsync(string incidentNumber, Guid bedId, Guid ambulanceId);
}
