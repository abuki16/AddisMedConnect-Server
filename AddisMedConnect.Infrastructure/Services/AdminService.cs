using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Enums;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly AddisDbContext _context;

    public AdminService(AddisDbContext context)
    {
        _context = context;
    }

    public async Task<AdminMetricsDto> GetMetricsAsync()
    {
        var totalIncidents = await _context.EmergencyCases.CountAsync();
        var dispatchedIncidents = await _context.EmergencyCases.CountAsync(c => c.Status == CaseStatus.Dispatched);
        var admittedIncidents = await _context.EmergencyCases.CountAsync(c => c.Status == CaseStatus.Admitted);
        var resolvedIncidents = await _context.EmergencyCases.CountAsync(c => c.Status == CaseStatus.Resolved);
        var activeIncidents = await _context.EmergencyCases.CountAsync(c => c.Status != CaseStatus.Resolved && c.Status != CaseStatus.Cancelled);

        var partnerHospitals = await _context.Hospitals.CountAsync();

        var fleetUnits = await _context.Ambulances.CountAsync();
        var availableAmbulances = await _context.Ambulances.CountAsync(a => a.IsAvailable);
        var dispatchedAmbulances = fleetUnits - availableAmbulances;

        var totalBeds = await _context.Beds.CountAsync();
        var availableBeds = await _context.Beds.CountAsync(b => b.Status == BedStatus.Available);
        var occupiedBeds = await _context.Beds.CountAsync(b => b.Status == BedStatus.Occupied);
        var cleaningBeds = await _context.Beds.CountAsync(b => b.Status == BedStatus.Maintenance || b.Status == BedStatus.Reserved);

        var recentCasesEntities = await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .Include(ec => ec.AssignedAmbulance)
            .OrderByDescending(ec => ec.CreatedAt)
            .Take(30)
            .ToListAsync();

        var recentCases = recentCasesEntities.Select(ec => new EmergencyCaseDto(
            ec.IncidentNumber,
            ec.CallerName,
            ec.CallerPhone,
            ec.PatientName,
            ec.IncidentReason,
            ec.Status,
            ec.Priority,
            ec.TargetHospitalId,
            ec.TargetHospital != null ? ec.TargetHospital.Name : "Hospital Pending",
            ec.AssignedBedId,
            ec.AssignedBed != null ? ec.AssignedBed.BedNumber : null,
            ec.AssignedAmbulanceId,
            ec.AssignedAmbulance != null ? ec.AssignedAmbulance.PlateNumber : null,
            ec.PickupAddress,
            ec.CreatedAt,
            ec.PickupLatitude,
            ec.PickupLongitude
        ));

        return new AdminMetricsDto(
            totalIncidents,
            activeIncidents,
            dispatchedIncidents,
            admittedIncidents,
            resolvedIncidents,
            partnerHospitals,
            fleetUnits,
            availableAmbulances,
            dispatchedAmbulances,
            0,
            totalBeds,
            availableBeds,
            occupiedBeds,
            cleaningBeds,
            recentCases
        );
    }
}
