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
            .Select(ec => new EmergencyCaseDto(
                ec.Id,
                ec.IncidentNumber,
                ec.PatientName,
                ec.ChiefComplaint,
                ec.Status,
                ec.Priority,
                ec.TargetHospitalId,
                ec.TargetHospital.Name,
                ec.AssignedBedId,
                ec.AssignedBed != null ? ec.AssignedBed.BedNumber : null,
                ec.PickupAddress,
                ec.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<EmergencyCaseDto?> GetCaseByIdAsync(Guid id)
    {
        var ec = await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (ec == null) return null;

        return new EmergencyCaseDto(
            ec.Id,
            ec.IncidentNumber,
            ec.PatientName,
            ec.ChiefComplaint,
            ec.Status,
            ec.Priority,
            ec.TargetHospitalId,
            ec.TargetHospital.Name,
            ec.AssignedBedId,
            ec.AssignedBed?.BedNumber,
            ec.PickupAddress,
            ec.CreatedAt
        );
    }

    public async Task<EmergencyCaseDto> CreateCaseAsync(CreateEmergencyCaseDto dto)
    {
        var incidentNo = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        var entity = new EmergencyCase
        {
            IncidentNumber = incidentNo,
            PatientName = dto.PatientName,
            ChiefComplaint = dto.ChiefComplaint,
            Priority = dto.Priority,
            TargetHospitalId = dto.TargetHospitalId,
            AssignedBedId = dto.AssignedBedId,
            Status = CaseStatus.PendingDispatch,
            PickupAddress = dto.PickupAddress,
            PickupLatitude = dto.PickupLatitude,
            PickupLongitude = dto.PickupLongitude,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmergencyCases.Add(entity);
        await _context.SaveChangesAsync();

        return await GetCaseByIdAsync(entity.Id) 
               ?? throw new InvalidOperationException("Failed to create emergency case.");
    }

    public async Task<bool> UpdateCaseStatusAsync(Guid id, CaseStatus status)
    {
        var ec = await _context.EmergencyCases.FindAsync(id);
        if (ec == null) return false;

        ec.Status = status;
        if (status == CaseStatus.Resolved)
        {
            ec.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}