using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Services;

public class AmbulanceService : IAmbulanceService
{
    private readonly AddisDbContext _context;

    public AmbulanceService(AddisDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Ambulance>> GetAllAsync()
    {
        return await _context.Ambulances
            .Include(a => a.DriverUser)
            .OrderBy(a => a.PlateNumber)
            .ToListAsync();
    }

    public async Task<Ambulance?> GetByIdAsync(Guid ambulanceId)
    {
        return await _context.Ambulances
            .Include(a => a.DriverUser)
            .FirstOrDefaultAsync(a => a.Id == ambulanceId);
    }

    public async Task<Ambulance> CreateAsync(CreateAmbulanceDto dto)
    {
        var plate = dto.PlateNumber.Trim();
        var existing = await _context.Ambulances
            .AnyAsync(a => a.PlateNumber.ToLower() == plate.ToLower());

        if (existing)
        {
            throw new InvalidOperationException($"An ambulance with plate number '{plate}' is already registered.");
        }

        var ambulance = new Ambulance
        {
            Id = Guid.NewGuid(),
            PlateNumber = plate,
            DriverName = string.IsNullOrWhiteSpace(dto.DriverName) ? null : dto.DriverName.Trim(),
            DriverUserId = dto.DriverUserId,
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            IsAvailable = dto.IsAvailable,
            CurrentLatitude = dto.CurrentLatitude ?? 9.0300,
            CurrentLongitude = dto.CurrentLongitude ?? 38.7400,
            LastLocationUpdatedAt = DateTime.UtcNow
        };

        _context.Ambulances.Add(ambulance);
        await _context.SaveChangesAsync();
        return ambulance;
    }

    public async Task<Ambulance?> UpdateAsync(Guid ambulanceId, UpdateAmbulanceDto dto)
    {
        var ambulance = await _context.Ambulances.FindAsync(ambulanceId);
        if (ambulance is null) return null;

        var plate = dto.PlateNumber.Trim();
        var plateClash = await _context.Ambulances
            .AnyAsync(a => a.Id != ambulanceId && a.PlateNumber.ToLower() == plate.ToLower());

        if (plateClash)
        {
            throw new InvalidOperationException($"Another ambulance with plate number '{plate}' already exists.");
        }

        ambulance.PlateNumber = plate;
        ambulance.DriverName = string.IsNullOrWhiteSpace(dto.DriverName) ? null : dto.DriverName.Trim();
        ambulance.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        ambulance.DriverUserId = dto.DriverUserId;
        ambulance.IsAvailable = dto.IsAvailable;
        if (dto.CurrentLatitude is not null && dto.CurrentLongitude is not null)
        {
            ambulance.CurrentLatitude = dto.CurrentLatitude;
            ambulance.CurrentLongitude = dto.CurrentLongitude;
            ambulance.LastLocationUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return ambulance;
    }

    public async Task<bool> UpdateStatusAsync(Guid ambulanceId, bool isAvailable)
    {
        var ambulance = await _context.Ambulances.FindAsync(ambulanceId);
        if (ambulance is null) return false;

        ambulance.IsAvailable = isAvailable;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid ambulanceId)
    {
        var ambulance = await _context.Ambulances.FindAsync(ambulanceId);
        if (ambulance is null) return false;

        var hasActiveCase = await _context.EmergencyCases.AnyAsync(c =>
            c.AssignedAmbulanceId == ambulanceId &&
            c.Status != CaseStatus.Resolved &&
            c.Status != CaseStatus.Cancelled);

        if (hasActiveCase)
        {
            throw new InvalidOperationException("Cannot delete ambulance while it is currently dispatched or on an active emergency mission.");
        }

        var pastCases = await _context.EmergencyCases
            .Where(c => c.AssignedAmbulanceId == ambulanceId)
            .ToListAsync();
        foreach (var c in pastCases)
        {
            c.AssignedAmbulanceId = null;
        }

        var locations = await _context.AmbulanceLocations
            .Where(l => l.AmbulanceId == ambulanceId)
            .ToListAsync();
        _context.AmbulanceLocations.RemoveRange(locations);

        _context.Ambulances.Remove(ambulance);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<object?> GetDriverAmbulanceAndAssignmentAsync(Guid driverUserId)
    {
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == driverUserId);
        if (ambulance is null) return null;

        var assignment = await _context.EmergencyCases
            .Include(c => c.TargetHospital)
            .Include(c => c.AssignedBed)
            .Where(c => c.AssignedAmbulanceId == ambulance.Id &&
                        (c.Status == CaseStatus.Dispatched ||
                         c.Status == CaseStatus.InTransit ||
                         c.Status == CaseStatus.ArrivedAtTriage))
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new
            {
                c.IncidentNumber,
                c.CallerName,
                c.CallerPhone,
                c.PatientName,
                c.IncidentReason,
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                c.PickupAddress,
                c.PickupLatitude,
                c.PickupLongitude,
                c.CreatedAt,
                TargetHospital = new
                {
                    c.TargetHospital.Id,
                    c.TargetHospital.Name,
                    c.TargetHospital.Address,
                    c.TargetHospital.SubCity,
                    c.TargetHospital.Latitude,
                    c.TargetHospital.Longitude
                },
                AssignedBed = c.AssignedBed != null ? new
                {
                    c.AssignedBed.Id,
                    c.AssignedBed.BedNumber,
                    c.AssignedBed.WardType
                } : null,
                AssignedBedNumber = c.AssignedBed != null ? c.AssignedBed.BedNumber : null
            })
            .FirstOrDefaultAsync();

        return new { ambulance, assignment };
    }

    public async Task<(bool Success, string Message, CaseStatus? NewStatus, Guid? HospitalId)> UpdateMissionStatusAsync(Guid driverUserId, CaseStatus status)
    {
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == driverUserId);
        if (ambulance is null) return (false, "No ambulance is linked to this driver account.", null, null);

        var activeCase = await _context.EmergencyCases
            .Where(c => c.AssignedAmbulanceId == ambulance.Id &&
                        (c.Status == CaseStatus.Dispatched ||
                         c.Status == CaseStatus.InTransit ||
                         c.Status == CaseStatus.ArrivedAtTriage))
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (activeCase is null) return (false, "No active emergency mission assigned.", null, null);

        if (status == CaseStatus.InTransit || status == CaseStatus.ArrivedAtTriage)
        {
            activeCase.Status = status;
            await _context.SaveChangesAsync();
            return (true, $"Mission status updated to {status}", status, activeCase.TargetHospitalId);
        }

        return (false, "Ambulance driver can only transition status to InTransit or ArrivedAtTriage.", null, null);
    }

    public async Task<bool> UpdateDriverLocationAsync(Guid driverUserId, UpdateAmbulanceLocationDto dto)
    {
        var ambulance = await _context.Ambulances.SingleOrDefaultAsync(a => a.DriverUserId == driverUserId);
        if (ambulance is null) return false;

        ambulance.CurrentLatitude = dto.Latitude;
        ambulance.CurrentLongitude = dto.Longitude;
        ambulance.LastLocationUpdatedAt = DateTime.UtcNow;

        var activeIncidentNumber = await _context.EmergencyCases
            .Where(c => c.AssignedAmbulanceId == ambulance.Id &&
                        c.Status != CaseStatus.Resolved &&
                        c.Status != CaseStatus.Cancelled)
            .Select(c => c.IncidentNumber)
            .FirstOrDefaultAsync();

        _context.AmbulanceLocations.Add(new AmbulanceLocation
        {
            AmbulanceId = ambulance.Id,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            AddressLabel = dto.AddressLabel,
            IncidentNumber = activeIncidentNumber
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<AmbulanceLocation>> GetLocationsAsync(Guid ambulanceId)
    {
        return await _context.AmbulanceLocations
            .Where(l => l.AmbulanceId == ambulanceId)
            .OrderByDescending(l => l.RecordedAt)
            .Take(100)
            .ToListAsync();
    }
}
