using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Services;

public class HospitalService : IHospitalService
{
    private readonly AddisDbContext _context;
    private readonly IBedNotificationService _notificationService;

    public HospitalService(AddisDbContext context, IBedNotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<HospitalDto>> GetAllHospitalsAsync()
    {
        return await _context.Hospitals
            .OrderBy(h => h.Name)
            .Select(h => new HospitalDto(
                h.Id,
                h.Code,
                h.Name,
                h.SubCity,
                h.Address,
                h.Latitude,
                h.Longitude,
                h.ContactPhone,
                h.Beds.Count,
                h.Beds.Count(b => b.Status == BedStatus.Available)
            ))
            .ToListAsync();
    }

    public async Task<HospitalDto?> GetHospitalByIdAsync(Guid id)
    {
        return await _context.Hospitals
            .Where(h => h.Id == id)
            .Select(h => new HospitalDto(
                h.Id,
                h.Code,
                h.Name,
                h.SubCity,
                h.Address,
                h.Latitude,
                h.Longitude,
                h.ContactPhone,
                h.Beds.Count,
                h.Beds.Count(b => b.Status == BedStatus.Available)
            ))
            .FirstOrDefaultAsync();
    }

    public async Task<HospitalDto> CreateHospitalAsync(CreateHospitalDto dto)
    {
        var codeTrimmed = dto.Code.Trim().ToUpperInvariant();
        var nameTrimmed = dto.Name.Trim();

        var codeExists = await _context.Hospitals.AnyAsync(h => h.Code.ToUpper() == codeTrimmed);
        if (codeExists)
        {
            throw new InvalidOperationException($"A hospital with code '{dto.Code}' already exists.");
        }

        var nameExists = await _context.Hospitals.AnyAsync(h => h.Name.ToLower() == nameTrimmed.ToLower());
        if (nameExists)
        {
            throw new InvalidOperationException($"A hospital named '{dto.Name}' is already registered.");
        }

        var hospital = new Hospital
        {
            Id = Guid.NewGuid(),
            Code = codeTrimmed,
            Name = nameTrimmed,
            SubCity = dto.SubCity.Trim(),
            Address = dto.Address.Trim(),
            Latitude = dto.Latitude ?? 9.0300,
            Longitude = dto.Longitude ?? 38.7400,
            ContactPhone = dto.ContactPhone.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();

        return new HospitalDto(
            hospital.Id,
            hospital.Code,
            hospital.Name,
            hospital.SubCity,
            hospital.Address,
            hospital.Latitude,
            hospital.Longitude,
            hospital.ContactPhone,
            0,
            0
        );
    }

    public async Task<HospitalDto?> UpdateHospitalAsync(Guid id, UpdateHospitalDto dto)
    {
        var hospital = await _context.Hospitals
            .Include(h => h.Beds)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hospital == null) return null;

        var codeTrimmed = dto.Code.Trim().ToUpperInvariant();
        var nameTrimmed = dto.Name.Trim();

        var codeClash = await _context.Hospitals.AnyAsync(h => h.Id != id && h.Code.ToUpper() == codeTrimmed);
        if (codeClash)
        {
            throw new InvalidOperationException($"Hospital code '{dto.Code}' is already used by another facility.");
        }

        var nameClash = await _context.Hospitals.AnyAsync(h => h.Id != id && h.Name.ToLower() == nameTrimmed.ToLower());
        if (nameClash)
        {
            throw new InvalidOperationException($"A hospital named '{dto.Name}' already exists.");
        }

        hospital.Code = codeTrimmed;
        hospital.Name = nameTrimmed;
        hospital.SubCity = dto.SubCity.Trim();
        hospital.Address = dto.Address.Trim();
        hospital.Latitude = dto.Latitude ?? hospital.Latitude;
        hospital.Longitude = dto.Longitude ?? hospital.Longitude;
        hospital.ContactPhone = dto.ContactPhone.Trim();

        await _context.SaveChangesAsync();

        return new HospitalDto(
            hospital.Id,
            hospital.Code,
            hospital.Name,
            hospital.SubCity,
            hospital.Address,
            hospital.Latitude,
            hospital.Longitude,
            hospital.ContactPhone,
            hospital.Beds.Count,
            hospital.Beds.Count(b => b.Status == BedStatus.Available)
        );
    }

    public async Task<bool> DeleteHospitalAsync(Guid id)
    {
        var hospital = await _context.Hospitals
            .Include(h => h.Beds)
            .FirstOrDefaultAsync(h => h.Id == id);
        if (hospital == null) return false;

        // 1. Check active emergency cases routed to this hospital
        var hasActiveCases = await _context.EmergencyCases.AnyAsync(c =>
            c.TargetHospitalId == id &&
            c.Status != CaseStatus.Resolved &&
            c.Status != CaseStatus.Cancelled);
        if (hasActiveCases)
        {
            throw new InvalidOperationException("Cannot delete hospital while active emergency incidents are routed to or awaiting triage at this facility.");
        }

        // 2. Check occupied/reserved beds
        var hasOccupiedBeds = hospital.Beds.Any(b =>
            b.Status == BedStatus.Occupied ||
            b.Status == BedStatus.Reserved ||
            b.CurrentCaseId != null);
        if (hasOccupiedBeds)
        {
            throw new InvalidOperationException("Cannot delete hospital with occupied or reserved patient beds.");
        }

        // 3. Check historical case references
        var hasHistoricalCases = await _context.EmergencyCases.AnyAsync(c => c.TargetHospitalId == id);
        if (hasHistoricalCases)
        {
            throw new InvalidOperationException("Cannot delete hospital that has recorded emergency incident history. In-flight and historical medical records must be preserved.");
        }

        // 4. Detach staff members assigned to this hospital
        var staff = await _context.Users.Where(u => u.AssignedHospitalId == id).ToListAsync();
        foreach (var user in staff)
        {
            user.AssignedHospitalId = null;
        }

        // 5. Delete empty/inactive beds belonging to this hospital
        _context.Beds.RemoveRange(hospital.Beds);

        // 6. Remove the hospital
        _context.Hospitals.Remove(hospital);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<BedDto>> GetBedsByHospitalIdAsync(Guid hospitalId)
    {
        return await _context.Beds
            .Where(b => b.HospitalId == hospitalId)
            .Select(b => new BedDto(
                b.Id,
                b.BedNumber,
                b.WardType,
                b.Status.ToString(),
                b.HospitalId,
                b.LastStatusUpdate
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<HospitalAvailabilityDto>> GetAvailableBedsAsync()
    {
        var hospitals = await _context.Hospitals
            .Include(h => h.Beds.Where(b => b.Status == BedStatus.Available))
            .ToListAsync();

        return hospitals.Select(h => new HospitalAvailabilityDto(
            h.Id,
            h.Name,
            h.SubCity,
            h.Beds.Select(b => new BedDto(
                b.Id,
                b.BedNumber,
                b.WardType,
                b.Status.ToString(),
                b.HospitalId,
                b.LastStatusUpdate
            )).ToList()
        ));
    }

    public async Task<bool> UpdateBedStatusAsync(Guid bedId, int status)
    {
        var bed = await _context.Beds.FindAsync(bedId);
        if (bed == null) return false;

        bed.Status = (BedStatus)status;
        bed.LastStatusUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Broadcast real-time update
        await _notificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, bed.Status.ToString());

        return true;
    }
}