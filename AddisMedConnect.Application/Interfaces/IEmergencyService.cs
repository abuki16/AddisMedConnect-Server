using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.Interfaces;

public interface IEmergencyService
{
    Task<IEnumerable<EmergencyCaseDto>> GetAllCasesAsync();
    Task<EmergencyCaseDto?> GetCaseByIncidentNumberAsync(string incidentNumber);
    Task<EmergencyCaseDto> CreateCaseAsync(CreateEmergencyCaseDto dto);
    Task<bool> UpdateCaseStatusAsync(string incidentNumber, CaseStatus status);
    Task<bool> CompleteTriageAsync(string incidentNumber, TriageAssessmentDto dto);
}