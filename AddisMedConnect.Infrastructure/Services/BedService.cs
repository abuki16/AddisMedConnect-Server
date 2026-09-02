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
}