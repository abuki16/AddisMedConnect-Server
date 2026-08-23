using System.ComponentModel.DataAnnotations;
using AddisMedConnect.Domain.Enums;

namespace AddisMedConnect.Application.DTOs;

public class UpdateCaseStatusDto
{
    [Required(ErrorMessage = "Case status is required.")]
    [EnumDataType(typeof(CaseStatus), ErrorMessage = "Invalid case status value.")]
    public CaseStatus Status { get; set; }
}