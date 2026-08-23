using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.DTOs;

public record TriageAssessmentDto(
    TriagePriority Priority,
    Guid ConfirmedBedId
);