using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
using AddisMedConnect.Domain.Entities;
using AddisMedConnect.Domain.Enums;
using AddisMedConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AddisMedConnect.Infrastructure.Services;

public class BedService : IBedService
{
    private readonly AddisDbContext _context;
    private readonly IBedNotificationService _bedNotificationService;

    public BedService(AddisDbContext context, IBedNotificationService bedNotificationService)
    {
        _context = context;
        _bedNotificationService = bedNotificationService;
    }

    public async Task<Bed> CreateBedAsync(CreateBedDto dto)
    {
        // Verify that the target hospital exists
        var hospitalExists = await _context.Hospitals.AnyAsync(h => h.Id == dto.HospitalId);
        if (!hospitalExists)
        {
            throw new KeyNotFoundException($"Hospital with ID '{dto.HospitalId}' was not found.");
        }

        var bed = new Bed
        {
            Id = Guid.NewGuid(),
            BedNumber = dto.BedNumber,
            WardType = dto.WardType,
            Code = dto.Code,
            HospitalId = dto.HospitalId,
            Status = BedStatus.Available,
            LastStatusUpdate = DateTime.UtcNow
        };

        _context.Beds.Add(bed);
        await _context.SaveChangesAsync();

        // Optionally include hospital details in the returned object
        return await _context.Beds
            .Include(b => b.Hospital)
            .FirstAsync(b => b.Id == bed.Id);
    }
    public async Task<IEnumerable<Bed>> GetAllBedsAsync()
    {
        return await _context.Beds
            .Include(b => b.Hospital)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bed>> GetBedsByHospitalIdAsync(Guid hospitalId)
    {
        return await _context.Beds
            .Where(b => b.HospitalId == hospitalId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Bed>> GetAvailableBedsByHospitalIdAsync(Guid hospitalId)
    {
        return await _context.Beds
            .Where(b => b.HospitalId == hospitalId && b.CurrentCaseId == null && b.Status == BedStatus.Available)
            .ToListAsync();
    }

    public async Task<bool> UpdateBedStatusAsync(Guid bedId, BedStatus status)
    {
        var bed = await _context.Beds.FindAsync(bedId);
        if (bed == null) return false;

        bed.Status = status;
        bed.LastStatusUpdate = DateTime.UtcNow;

        if (status == BedStatus.Available)
        {
            bed.CurrentCaseId = null;
        }

        await _context.SaveChangesAsync();

        // 🚀 Broadcast real-time update using your SignalR notification service
        await _bedNotificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, bed.Status.ToString());

        return true;
    }

    public async Task<Bed?> UpdateBedAsync(Guid bedId, UpdateBedDto dto)
    {
        var bed = await _context.Beds
            .Include(b => b.Hospital)
            .FirstOrDefaultAsync(b => b.Id == bedId);
        if (bed == null) return null;

        var hospitalExists = await _context.Hospitals.AnyAsync(h => h.Id == dto.HospitalId);
        if (!hospitalExists)
        {
            throw new KeyNotFoundException($"Hospital with ID '{dto.HospitalId}' was not found.");
        }

        // Check if code is changed and whether it clashes with another bed
        var codeInUse = await _context.Beds.AnyAsync(b => b.Id != bedId && b.Code.ToLower() == dto.Code.ToLower());
        if (codeInUse)
        {
            throw new InvalidOperationException($"Bed code '{dto.Code}' is already in use by another bed.");
        }

        var oldStatus = bed.Status;
        bed.BedNumber = dto.BedNumber.Trim();
        bed.WardType = dto.WardType.Trim();
        bed.Code = dto.Code.Trim();
        bed.HospitalId = dto.HospitalId;
        bed.Status = dto.Status;
        bed.LastStatusUpdate = DateTime.UtcNow;

        if (dto.Status == BedStatus.Available)
        {
            bed.CurrentCaseId = null;
        }

        await _context.SaveChangesAsync();

        if (oldStatus != dto.Status)
        {
            await _bedNotificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, bed.Status.ToString());
        }

        return bed;
    }

    public async Task<bool> DeleteBedAsync(Guid bedId)
    {
        var bed = await _context.Beds.FindAsync(bedId);
        if (bed == null) return false;

        // Check if bed is actively occupied or reserved
        if (bed.Status == BedStatus.Occupied || bed.Status == BedStatus.Reserved || bed.CurrentCaseId != null)
        {
            throw new InvalidOperationException("Cannot delete bed while it is currently occupied or reserved by an emergency patient.");
        }

        // Check if referenced by an active, non-completed emergency case
        var hasActiveCase = await _context.EmergencyCases.AnyAsync(c =>
            c.AssignedBedId == bedId &&
            c.Status != CaseStatus.Resolved &&
            c.Status != CaseStatus.Cancelled);

        if (hasActiveCase)
        {
            throw new InvalidOperationException("Cannot delete bed because it is linked to an active emergency incident.");
        }

        // For past resolved/cancelled cases, detach the bed reference before deleting
        var historicalCases = await _context.EmergencyCases
            .Where(c => c.AssignedBedId == bedId)
            .ToListAsync();
        foreach (var c in historicalCases)
        {
            c.AssignedBedId = null;
        }

        _context.Beds.Remove(bed);
        await _context.SaveChangesAsync();

        // Broadcast removal via SignalR so clients immediately remove or update the bed
        await _bedNotificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, "Deleted");

        return true;
    }
}