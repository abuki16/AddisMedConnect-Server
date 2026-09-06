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
    private readonly IBedNotificationService _bedNotificationService;

    public EmergencyService(AddisDbContext context, IBedNotificationService bedNotificationService)
    {
        _context = context;
        _bedNotificationService = bedNotificationService;
    }

    public async Task<IEnumerable<EmergencyCaseDto>> GetAllCasesAsync()
    {
        return await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .Include(ec => ec.AssignedAmbulance)
            .Select(ec => new EmergencyCaseDto(
                ec.IncidentNumber,
                ec.CallerName,
                ec.CallerPhone,
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
                ec.CreatedAt,
                ec.PickupLatitude,
                ec.PickupLongitude
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<EmergencyCaseDto>> GetCasesByHospitalAsync(Guid hospitalId)
    {
        return await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .Include(ec => ec.AssignedAmbulance)
            .Where(ec => ec.TargetHospitalId == hospitalId &&
                         (ec.Status == CaseStatus.Dispatched ||
                          ec.Status == CaseStatus.InTransit ||
                          ec.Status == CaseStatus.ArrivedAtTriage ||
                          ec.Status == CaseStatus.PendingTriage))
            .Select(ec => new EmergencyCaseDto(
                ec.IncidentNumber,
                ec.CallerName,
                ec.CallerPhone,
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
                ec.CreatedAt,
                ec.PickupLatitude,
                ec.PickupLongitude
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<EmergencyCaseDto>> GetActiveHospitalCasesAsync(Guid hospitalId)
    {
        return await _context.EmergencyCases
            .Include(ec => ec.TargetHospital)
            .Include(ec => ec.AssignedBed)
            .Include(ec => ec.AssignedAmbulance)
            .Where(ec => ec.TargetHospitalId == hospitalId &&
                         (ec.Status == CaseStatus.Dispatched ||
                          ec.Status == CaseStatus.InTransit ||
                          ec.Status == CaseStatus.ArrivedAtTriage ||
                          ec.Status == CaseStatus.PendingTriage ||
                          ec.Status == CaseStatus.Admitted))
            .Select(ec => new EmergencyCaseDto(
                ec.IncidentNumber,
                ec.CallerName,
                ec.CallerPhone,
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
                ec.CreatedAt,
                ec.PickupLatitude,
                ec.PickupLongitude
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
            ec.CallerName,
            ec.CallerPhone,
            ec.PatientName,
            ec.IncidentReason,
            ec.Status,
            ec.Priority,
            ec.TargetHospitalId,
            ec.TargetHospital.Name,
            ec.AssignedBedId,
            ec.AssignedBed != null ? ec.AssignedBed.BedNumber : null,
            ec.AssignedAmbulanceId,
            ec.AssignedAmbulance?.PlateNumber,
            ec.PickupAddress,
            ec.CreatedAt,
            ec.PickupLatitude,
            ec.PickupLongitude
        );
    }

    public async Task<int> GetPendingTriageCountAsync()
    {
        return await _context.EmergencyCases
            .CountAsync(c => c.Status == CaseStatus.Dispatched ||
                             c.Status == CaseStatus.InTransit ||
                             c.Status == CaseStatus.ArrivedAtTriage ||
                             c.Status == CaseStatus.PendingTriage);
    }

    public async Task<int> GetPendingTriageCountByHospitalAsync(Guid hospitalId)
    {
        return await _context.EmergencyCases
            .CountAsync(ec => ec.TargetHospitalId == hospitalId &&
                              (ec.Status == CaseStatus.Dispatched ||
                               ec.Status == CaseStatus.InTransit ||
                               ec.Status == CaseStatus.ArrivedAtTriage ||
                               ec.Status == CaseStatus.PendingTriage));
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

        var incidentNo = $"INC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        Guid? resolvedBedGuid = null;
        if (!string.IsNullOrEmpty(dto.AssignedBedId))
        {
            Bed? bed = null;
            if (Guid.TryParse(dto.AssignedBedId, out var parsedBedId))
            {
                bed = await _context.Beds
                    .Include(b => b.Hospital)
                    .FirstOrDefaultAsync(b => b.Id == parsedBedId && b.HospitalId == hospital.Id);
            }

            if (bed == null)
            {
                bed = await _context.Beds
                    .Include(b => b.Hospital)
                    .FirstOrDefaultAsync(b => b.Code == dto.AssignedBedId && b.HospitalId == hospital.Id);
            }

            if (bed == null)
            {
                throw new KeyNotFoundException($"Bed with identifier '{dto.AssignedBedId}' was not found in hospital '{hospital.Name}'.");
            }

            bed.Status = BedStatus.Reserved;
            bed.CurrentCaseId = incidentNo;
            bed.LastStatusUpdate = DateTime.UtcNow;
            resolvedBedGuid = bed.Id;
            await _bedNotificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, BedStatus.Reserved.ToString());
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

            ambulance.IsAvailable = false;
            resolvedAmbulanceGuid = ambulance.Id;
        }

        var entity = new EmergencyCase
        {
            IncidentNumber = incidentNo,
            CallerName = string.IsNullOrWhiteSpace(dto.CallerName) ? "Unknown Caller" : dto.CallerName,
            CallerPhone = string.IsNullOrWhiteSpace(dto.CallerPhone) ? "N/A" : dto.CallerPhone,
            PatientName = string.IsNullOrWhiteSpace(dto.PatientName) ? "Unknown Patient" : dto.PatientName,
            IncidentReason = dto.IncidentReason,
            Priority = TriagePriority.Yellow,
            TargetHospitalId = hospital.Id,
            AssignedBedId = resolvedBedGuid,
            AssignedAmbulanceId = resolvedAmbulanceGuid,
            Status = CaseStatus.Dispatched,
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
                .ThenInclude(b => b!.Hospital)
            .FirstOrDefaultAsync(x => x.IncidentNumber == incidentNumber);

        if (ec == null) return false;

        ec.Status = status;

        if (status == CaseStatus.Cancelled || status == CaseStatus.Resolved)
        {
            ec.CompletedAt = DateTime.UtcNow;

            if (ec.AssignedBed != null)
            {
                var hId = ec.AssignedBed.HospitalId;
                var bId = ec.AssignedBed.Id;
                ec.AssignedBed.Status = BedStatus.Available;
                ec.AssignedBed.LastStatusUpdate = DateTime.UtcNow;
                ec.AssignedBed.CurrentCaseId = null;
                ec.AssignedBedId = null;
                await _bedNotificationService.NotifyBedStatusChangedAsync(hId, bId, BedStatus.Available.ToString());
            }
            else if (ec.AssignedBedId.HasValue)
            {
                var bed = await _context.Beds.FindAsync(ec.AssignedBedId.Value);
                if (bed != null)
                {
                    bed.Status = BedStatus.Available;
                    bed.LastStatusUpdate = DateTime.UtcNow;
                    bed.CurrentCaseId = null;
                    await _bedNotificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, BedStatus.Available.ToString());
                }
                ec.AssignedBedId = null;
            }

            if (ec.AssignedAmbulanceId is not null)
            {
                var ambulance = await _context.Ambulances.FindAsync(ec.AssignedAmbulanceId);
                if (ambulance is not null)
                {
                    ambulance.IsAvailable = true;
                    var hospital = await _context.Hospitals.FindAsync(ec.TargetHospitalId);
                    if (hospital != null && hospital.Latitude.HasValue && hospital.Longitude.HasValue)
                    {
                        ambulance.CurrentLatitude = hospital.Latitude.Value;
                        ambulance.CurrentLongitude = hospital.Longitude.Value;
                        ambulance.LastLocationUpdatedAt = DateTime.UtcNow;
                    }

                    _context.AmbulanceLocations.Add(new AmbulanceLocation
                    {
                        AmbulanceId = ambulance.Id,
                        Latitude = ambulance.CurrentLatitude ?? 9.0300,
                        Longitude = ambulance.CurrentLongitude ?? 38.7400,
                        AddressLabel = hospital != null
                            ? $"Stationed at {hospital.Name} ({hospital.Address ?? hospital.SubCity})"
                            : "Released after mission completion",
                        IncidentNumber = ec.IncidentNumber,
                        RecordedAt = DateTime.UtcNow
                    });
                }
                ec.AssignedAmbulanceId = null;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CompleteTriageAsync(string incidentNumber, TriageAssessmentDto dto)
    {
        var ec = await _context.EmergencyCases
            .Include(c => c.AssignedBed)
                .ThenInclude(b => b!.Hospital)
            .FirstOrDefaultAsync(x => x.IncidentNumber == incidentNumber);

        if (ec == null || (ec.Status != CaseStatus.Dispatched &&
                           ec.Status != CaseStatus.InTransit &&
                           ec.Status != CaseStatus.ArrivedAtTriage &&
                           ec.Status != CaseStatus.PendingTriage)) return false;

        ec.Priority = dto.Priority;
        ec.Status = CaseStatus.Admitted;

        if (!string.IsNullOrWhiteSpace(dto.VitalSigns))
        {
            ec.IncidentReason = $"{ec.IncidentReason} | [Vitals: {dto.VitalSigns.Trim()}]";
        }
        if (!string.IsNullOrWhiteSpace(dto.TriageNotes))
        {
            ec.IncidentReason = $"{ec.IncidentReason} | [Triage: {dto.TriageNotes.Trim()}]";
        }

        var targetBedId = dto.ConfirmedBedId ?? ec.AssignedBedId;
        if (targetBedId.HasValue)
        {
            // If the triage nurse selected an alternative bed than previously assigned, release the previous bed
            if (ec.AssignedBedId.HasValue && ec.AssignedBedId.Value != targetBedId.Value)
            {
                var prevBed = await _context.Beds.FindAsync(ec.AssignedBedId.Value);
                if (prevBed != null)
                {
                    prevBed.Status = BedStatus.Available;
                    prevBed.CurrentCaseId = null;
                    prevBed.LastStatusUpdate = DateTime.UtcNow;
                    await _bedNotificationService.NotifyBedStatusChangedAsync(prevBed.HospitalId, prevBed.Id, BedStatus.Available.ToString());
                }
            }

            var targetBed = await _context.Beds.FindAsync(targetBedId.Value);
            if (targetBed != null)
            {
                if (targetBed.HospitalId != ec.TargetHospitalId)
                    throw new InvalidOperationException("The selected bed belongs to a different hospital.");

                if (ec.AssignedBedId != targetBed.Id && targetBed.Status != BedStatus.Available)
                    throw new InvalidOperationException("The selected bed is no longer available at this hospital.");

                targetBed.Status = BedStatus.Occupied;
                targetBed.CurrentCaseId = incidentNumber;
                targetBed.LastStatusUpdate = DateTime.UtcNow;
                ec.AssignedBedId = targetBed.Id;

                await _bedNotificationService.NotifyBedStatusChangedAsync(targetBed.HospitalId, targetBed.Id, BedStatus.Occupied.ToString());
            }
        }

        // The patient is now checked in. Release ambulance if requested.
        if (dto.ReleaseAmbulance && ec.AssignedAmbulanceId is not null)
        {
            var ambulance = await _context.Ambulances.FindAsync(ec.AssignedAmbulanceId);
            if (ambulance is not null)
            {
                ambulance.IsAvailable = true;

                // Associate the ambulance location to the hospital where the triage nurse worked
                var hospital = await _context.Hospitals.FindAsync(ec.TargetHospitalId);
                if (hospital != null && hospital.Latitude.HasValue && hospital.Longitude.HasValue)
                {
                    ambulance.CurrentLatitude = hospital.Latitude.Value;
                    ambulance.CurrentLongitude = hospital.Longitude.Value;
                    ambulance.LastLocationUpdatedAt = DateTime.UtcNow;
                }

                _context.AmbulanceLocations.Add(new AmbulanceLocation
                {
                    AmbulanceId = ambulance.Id,
                    Latitude = ambulance.CurrentLatitude ?? 9.0300,
                    Longitude = ambulance.CurrentLongitude ?? 38.7400,
                    AddressLabel = hospital != null
                        ? $"Stationed at {hospital.Name} ({hospital.Address ?? hospital.SubCity})"
                        : "Released after hospital triage arrival",
                    IncidentNumber = ec.IncidentNumber,
                    RecordedAt = DateTime.UtcNow
                });
            }
            ec.AssignedAmbulanceId = null;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<HospitalCapacityResultDto> CheckCapacityAndFindAlternativeAsync(Guid hospitalId, string wardType, double currentLat, double currentLng)
    {
        var freeBedsCount = await _context.Beds
            .CountAsync(b => b.HospitalId == hospitalId && b.WardType == wardType && b.Status == BedStatus.Available);

        if (freeBedsCount > 0)
        {
            return new HospitalCapacityResultDto
            {
                IsAvailable = true,
                Message = "Beds are available at the requested hospital."
            };
        }

        var hospitalsList = await _context.Hospitals
            .Where(h => h.Id != hospitalId && h.Beds.Any(b => b!.WardType == wardType && b!.Status == BedStatus.Available))
            .Select(h => new
            {
                Hospital = h,
                AvailableBedsCount = h.Beds.Count(b => b!.WardType == wardType && b!.Status == BedStatus.Available),
                Lat = h.Latitude,
                Lng = h.Longitude
            })
            .ToListAsync();

        var nearestHospitalWithBeds = hospitalsList
            .Select(x => new
            {
                x.Hospital,
                x.AvailableBedsCount,
                DistanceKm = Math.Sqrt(Math.Pow((x.Lat ?? 0.0) - currentLat, 2) + Math.Pow((x.Lng ?? 0.0) - currentLng, 2)) * 111
            })
            .OrderBy(x => x.DistanceKm)
            .FirstOrDefault();

        return new HospitalCapacityResultDto
        {
            IsAvailable = false,
            Message = "Selected hospital has no free beds for this ward type.",
            AlternativeHospital = nearestHospitalWithBeds?.Hospital,
            DistanceKm = nearestHospitalWithBeds?.DistanceKm ?? 0,
            AvailableBedsCount = nearestHospitalWithBeds?.AvailableBedsCount ?? 0
        };
    }

    public async Task<IEnumerable<HospitalRecommendationDto>> FindRecommendedHospitalsAsync(string wardType, double latitude, double longitude)
    {
        var hospitals = await _context.Hospitals
            .Select(h => new { h.Id, h.Name, h.SubCity, h.Address, h.Latitude, h.Longitude, AvailableBeds = h.Beds.Count(b => b.Status == BedStatus.Available), WardBeds = h.Beds.Count(b => b.Status == BedStatus.Available && b.WardType == wardType) })
            .ToListAsync();

        return hospitals.Where(h => h.Latitude.HasValue && h.Longitude.HasValue)
            .Select(h => new HospitalRecommendationDto(h.Id, h.Name, h.SubCity, h.Address,
                HaversineKilometres(latitude, longitude, h.Latitude!.Value, h.Longitude!.Value), h.AvailableBeds, h.WardBeds > 0,
                h.WardBeds > 0 ? "Recommended - requested ward has a free bed" : "No free requested-ward bed"))
            .OrderByDescending(h => h.HasRequestedWardCapacity).ThenBy(h => h.DistanceKm).ToList();
    }

    public async Task<List<Ambulance>> GetAvailableAmbulancesAsync()
    {
        var busyAmbulanceIds = await _context.EmergencyCases
            .Where(ec => ec.AssignedAmbulanceId != null && ec.Status != CaseStatus.Resolved && ec.Status != CaseStatus.Cancelled)
            .Select(ec => ec.AssignedAmbulanceId!.Value)
            .ToListAsync();

        return await _context.Ambulances
            .Where(a => !busyAmbulanceIds.Contains(a.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<RecommendedAmbulanceDto>> GetRecommendedAmbulancesAsync(string incidentNumber)
    {
        var emergencyCase = await _context.EmergencyCases
            .Include(c => c.TargetHospital)
            .FirstOrDefaultAsync(c => c.IncidentNumber == incidentNumber);

        if (emergencyCase == null)
            throw new KeyNotFoundException($"Emergency case {incidentNumber} not found.");

        var targetHospital = emergencyCase.TargetHospital;
        var hLat = targetHospital?.Latitude;
        var hLng = targetHospital?.Longitude;
        var hName = targetHospital?.Name ?? "Assigned Hospital";

        var pLat = emergencyCase.PickupLatitude;
        var pLng = emergencyCase.PickupLongitude;

        // An ambulance is busy if assigned to another active (unresolved, uncancelled) case
        var busyAmbulanceIds = await _context.EmergencyCases
            .Where(ec => ec.AssignedAmbulanceId != null &&
                         ec.IncidentNumber != incidentNumber &&
                         ec.Status != CaseStatus.Resolved &&
                         ec.Status != CaseStatus.Cancelled)
            .Select(ec => ec.AssignedAmbulanceId!.Value)
            .ToListAsync();

        var availableAmbulances = await _context.Ambulances
            .Where(a => !busyAmbulanceIds.Contains(a.Id))
            .ToListAsync();

        var items = new List<RecommendedAmbulanceDto>();

        foreach (var amb in availableAmbulances)
        {
            double? distHosp = null;
            if (hLat.HasValue && hLng.HasValue && amb.CurrentLatitude.HasValue && amb.CurrentLongitude.HasValue)
            {
                distHosp = Math.Round(HaversineKilometres(amb.CurrentLatitude.Value, amb.CurrentLongitude.Value, hLat.Value, hLng.Value), 2);
            }

            double? distPatient = null;
            if (pLat.HasValue && pLng.HasValue && amb.CurrentLatitude.HasValue && amb.CurrentLongitude.HasValue)
            {
                distPatient = Math.Round(HaversineKilometres(amb.CurrentLatitude.Value, amb.CurrentLongitude.Value, pLat.Value, pLng.Value), 2);
            }
            else if (distHosp.HasValue)
            {
                distPatient = distHosp;
            }

            int? etaMinutes = null;
            if (distPatient.HasValue)
            {
                etaMinutes = Math.Max(3, (int)Math.Ceiling(distPatient.Value / 30.0 * 60)); // ~30 km/h city average
            }

            // Within 400m of target hospital grounds
            var isAtTargetHospital = distHosp.HasValue && distHosp.Value <= 0.40;

            string badge;
            string reason;
            int priority;

            if (isAtTargetHospital)
            {
                badge = "🏥 Stationed at Assigned Hospital";
                reason = $"Unit currently at {hName}. Instant deployment from destination hospital grounds (0 km transfer).";
                priority = 1;
            }
            else
            {
                badge = "🚑 Active Fleet Unit";
                reason = distPatient.HasValue
                    ? $"Located {distPatient.Value:F1} km from scene (~{etaMinutes} min ETA)."
                    : "Available fleet vehicle ready for dispatch.";
                priority = 3;
            }

            items.Add(new RecommendedAmbulanceDto(
                amb.Id,
                amb.PlateNumber,
                amb.DriverName,
                amb.PhoneNumber,
                amb.CurrentLatitude,
                amb.CurrentLongitude,
                amb.IsAvailable,
                isAtTargetHospital,
                isAtTargetHospital ? hName : null,
                distHosp,
                distPatient,
                etaMinutes,
                badge,
                reason,
                priority
            ));
        }

        // Among non-hospital stationed units, identify the closest to patient
        var nonHospitalUnits = items.Where(i => !i.IsAtTargetHospital && i.DistanceToPatientKm.HasValue).ToList();
        if (nonHospitalUnits.Count > 0)
        {
            var closest = nonHospitalUnits.OrderBy(i => i.DistanceToPatientKm!.Value).First();
            var index = items.FindIndex(i => i.Id == closest.Id);
            if (index >= 0)
            {
                items[index] = items[index] with
                {
                    RecommendationBadge = "⚡ Nearest to Patient",
                    RecommendationReason = $"Fastest wheels-to-scene unit ({closest.DistanceToPatientKm:F1} km, ~{closest.EstimatedMinutesToPatient} min ETA).",
                    PriorityRank = 2
                };
            }
        }

        return items
            .OrderBy(i => i.PriorityRank)
            .ThenBy(i => i.DistanceToPatientKm ?? double.MaxValue)
            .ThenBy(i => i.PlateNumber)
            .ToList();
    }

    public async Task<EmergencyCaseDto> AssignResourcesAsync(string incidentNumber, Guid bedId, Guid ambulanceId)
    {
        var emergencyCase = await _context.EmergencyCases
            .Include(c => c.TargetHospital)
            .FirstOrDefaultAsync(c => c.IncidentNumber == incidentNumber);

        if (emergencyCase == null)
            throw new KeyNotFoundException($"Emergency case {incidentNumber} not found.");

        if (emergencyCase.AssignedBedId.HasValue && emergencyCase.AssignedBedId != bedId)
        {
            var oldBed = await _context.Beds
                .Include(b => b.Hospital)
                .FirstOrDefaultAsync(b => b.Id == emergencyCase.AssignedBedId.Value);

            if (oldBed != null)
            {
                oldBed.Status = BedStatus.Available;
                oldBed.CurrentCaseId = null;
                oldBed.LastStatusUpdate = DateTime.UtcNow;
                await _bedNotificationService.NotifyBedStatusChangedAsync(oldBed.HospitalId, oldBed.Id, BedStatus.Available.ToString());
            }
        }

        var bed = await _context.Beds
            .Include(b => b.Hospital)
            .FirstOrDefaultAsync(b => b.Id == bedId);

        if (bed == null || (bed.Status != BedStatus.Available && emergencyCase.AssignedBedId != bedId))
            throw new InvalidOperationException("Selected bed is unavailable or does not exist.");

        bed.Status = BedStatus.Reserved;
        bed.CurrentCaseId = incidentNumber;
        bed.LastStatusUpdate = DateTime.UtcNow;
        await _bedNotificationService.NotifyBedStatusChangedAsync(bed.HospitalId, bed.Id, BedStatus.Reserved.ToString());

        var ambulance = await _context.Ambulances.FirstOrDefaultAsync(a => a.Id == ambulanceId);
        if (ambulance == null)
            throw new InvalidOperationException("Selected ambulance does not exist.");

        var isAmbulanceBusy = await _context.EmergencyCases
            .AnyAsync(ec => ec.AssignedAmbulanceId == ambulanceId && ec.IncidentNumber != incidentNumber && ec.Status != CaseStatus.Resolved && ec.Status != CaseStatus.Cancelled);

        if (isAmbulanceBusy)
            throw new InvalidOperationException("Selected ambulance is currently assigned to another active case.");

        emergencyCase.AssignedBedId = bedId;
        emergencyCase.AssignedAmbulanceId = ambulanceId;
        ambulance.IsAvailable = false;
        emergencyCase.Status = CaseStatus.Dispatched;

        await _context.SaveChangesAsync();

        return await GetCaseByIncidentNumberAsync(incidentNumber) ?? throw new InvalidOperationException("Failed to retrieve updated case.");
    }

    private Task SendSmsAsync(string phoneNumber, string message)
    {
        Console.WriteLine($"[SMS GATEWAY SIMULATION] Sending to {phoneNumber}: \"{message}\"");
        return Task.CompletedTask;
    }

    private static double HaversineKilometres(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadius = 6371;
        var dLat = DegreesToRadians(lat2 - lat1); var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return earthRadius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
    private static double DegreesToRadians(double value) => value * Math.PI / 180;
}
