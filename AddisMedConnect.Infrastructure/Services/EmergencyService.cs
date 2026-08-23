using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Services;

public class EmergencyService : IEmergencyService
{
    private readonly AddisDbContext _context;

    public EmergencyService(AddisDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmergencyCaseDto>> GetAllCasesAsync()
    {
        return await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .Include(ec => ec.AssignedAmbulance)
            .Select(ec => new EmergencyCaseDto(
                ec.IncidentNumber,
                ec.PatientName,
                ec.IncidentReason,
                ec.Status,
                ec.Priority,
                ec.TargetHospitalId,
                ec.TargetHospital.Name,
                ec.AssignedBedId,
                ec.AssignedBed != null ? ec.AssignedBed.BedNumber : null,
                ec.AssignedAmbulanceId,
                ec.AssignedAmbulance != null ? ec.AssignedAmbulance.PlateNumber : null,
                ec.PickupAddress,
                ec.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<EmergencyCaseDto?> GetCaseByIncidentNumberAsync(string incidentNumber)
    {
        var ec = await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .Include(ec => ec.AssignedAmbulance)
            .FirstOrDefaultAsync(x => x.IncidentNumber == incidentNumber);

        if (ec == null) return null;

        return new EmergencyCaseDto(
            ec.IncidentNumber,
            ec.PatientName,
            ec.IncidentReason,
            ec.Status,
            ec.Priority,
            ec.TargetHospitalId,
            ec.TargetHospital.Name,
            ec.AssignedBedId,
            ec.AssignedBed?.BedNumber,
            ec.AssignedAmbulanceId,
            ec.AssignedAmbulance?.PlateNumber,
            ec.PickupAddress,
            ec.CreatedAt
        );
    }

    public async Task<EmergencyCaseDto> CreateCaseAsync(CreateEmergencyCaseDto dto)
    {
        Hospital? hospital = null;
        if (Guid.TryParse(dto.TargetHospitalId, out var parsedHospitalId))
        {
            hospital = await _context.Hospitals.FindAsync(parsedHospitalId);
        }

        if (hospital == null)
        {
            hospital = await _context.Hospitals
                .FirstOrDefaultAsync(h => h.Code == dto.TargetHospitalId || h.Name.ToLower().Contains(dto.TargetHospitalId.ToLower()));
        }

        if (hospital == null)
        {
            throw new KeyNotFoundException($"Hospital with identifier '{dto.TargetHospitalId}' was not found.");
        }

        Guid? resolvedBedGuid = null;
        if (!string.IsNullOrEmpty(dto.AssignedBedId))
        {
            Bed? bed = null;
            if (Guid.TryParse(dto.AssignedBedId, out var parsedBedId))
            {
                bed = await _context.Beds
                    .FirstOrDefaultAsync(b => b.Id == parsedBedId && b.HospitalId == hospital.Id);
            }

            if (bed == null)
            {
                bed = await _context.Beds
                    .FirstOrDefaultAsync(b => b.Code == dto.AssignedBedId && b.HospitalId == hospital.Id);
            }

            if (bed == null)
            {
                throw new KeyNotFoundException($"Bed with identifier '{dto.AssignedBedId}' was not found in hospital '{hospital.Name}'.");
            }

            bed.Status = BedStatus.Reserved; 
            bed.LastStatusUpdate = DateTime.UtcNow;
            resolvedBedGuid = bed.Id;
        }

        Guid? resolvedAmbulanceGuid = null;
        if (!string.IsNullOrEmpty(dto.AssignedAmbulanceId))
        {
            Ambulance? ambulance = null;
            if (Guid.TryParse(dto.AssignedAmbulanceId, out var parsedAmbulanceId))
            {
                ambulance = await _context.Ambulances.FindAsync(parsedAmbulanceId);
            }

            if (ambulance == null)
            {
                ambulance = await _context.Ambulances
                    .FirstOrDefaultAsync(a => 
                        a.PlateNumber.ToLower() == dto.AssignedAmbulanceId.ToLower() || 
                        a.PlateNumber.ToLower().Contains(dto.AssignedAmbulanceId.ToLower()));
            }

            if (ambulance == null)
            {
                throw new KeyNotFoundException($"Ambulance with identifier '{dto.AssignedAmbulanceId}' was not found.");
            }

            resolvedAmbulanceGuid = ambulance.Id;
        }

        var incidentNo = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        var initialStatus = resolvedAmbulanceGuid != null ? CaseStatus.Dispatched : CaseStatus.PendingDispatch;

        var entity = new EmergencyCase
        {
            IncidentNumber = incidentNo,
            PatientName = dto.PatientName,
            IncidentReason = dto.IncidentReason,
            Priority = 0, 
            TargetHospitalId = hospital.Id,   
            AssignedBedId = resolvedBedGuid,  
            AssignedAmbulanceId = resolvedAmbulanceGuid, 
            Status = initialStatus,
            PickupAddress = dto.PickupAddress,
            PickupLatitude = dto.PickupLatitude,
            PickupLongitude = dto.PickupLongitude,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmergencyCases.Add(entity);
        await _context.SaveChangesAsync();

        return await GetCaseByIncidentNumberAsync(entity.IncidentNumber) 
                ?? throw new InvalidOperationException("Failed to create emergency case.");
    }

    public async Task<bool> UpdateCaseStatusAsync(string incidentNumber, CaseStatus status)
    {
        var ec = await _context.EmergencyCases
            .Include(c => c.AssignedBed)
            .FirstOrDefaultAsync(x => x.IncidentNumber == incidentNumber);

        if (ec == null) return false;

        ec.Status = status;

        if (status == CaseStatus.Cancelled || status == CaseStatus.Resolved)
        {
            ec.CompletedAt = DateTime.UtcNow;

            if (ec.AssignedBed != null)
            {
                ec.AssignedBed.Status = BedStatus.Available; 
                ec.AssignedBed.LastStatusUpdate = DateTime.UtcNow;
                ec.AssignedBedId = null;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteTriageAsync(string incidentNumber, TriageAssessmentDto dto)
    {
        var ec = await _context.EmergencyCases.FindAsync(incidentNumber);
        if (ec == null) return false;

        ec.Priority = dto.Priority;
        ec.AssignedBedId = dto.ConfirmedBedId; 
        ec.Status = CaseStatus.ArrivedAtTriage; 

        var bed = await _context.Beds.FindAsync(dto.ConfirmedBedId);
        if (bed != null)
        {
            bed.Status = BedStatus.Occupied;
            bed.LastStatusUpdate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}