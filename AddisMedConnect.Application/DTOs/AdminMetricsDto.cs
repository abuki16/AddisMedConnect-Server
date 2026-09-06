using System;
using System.Collections.Generic;

namespace AddisMedConnect.Application.DTOs;

public record AdminMetricsDto(
    int TotalIncidents,
    int ActiveIncidents,
    int DispatchedIncidents,
    int AdmittedIncidents,
    int ResolvedIncidents,
    int PartnerHospitals,
    int FleetUnits,
    int AvailableAmbulances,
    int DispatchedAmbulances,
    int MaintenanceAmbulances,
    int TotalBeds,
    int AvailableBeds,
    int OccupiedBeds,
    int CleaningBeds,
    IEnumerable<EmergencyCaseDto> RecentCases
);
