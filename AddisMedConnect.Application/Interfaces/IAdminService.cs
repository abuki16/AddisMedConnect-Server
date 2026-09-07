using AddisMedConnect.Application.DTOs;

namespace AddisMedConnect.Application.Interfaces;

public interface IAdminService
{
    Task<AdminMetricsDto> GetMetricsAsync();
}
