using AddisMedConnect.Application.DTOs;
using AddisMedConnect.Application.Interfaces;
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
            .Select(h => new HospitalDto(
                h.Id,
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