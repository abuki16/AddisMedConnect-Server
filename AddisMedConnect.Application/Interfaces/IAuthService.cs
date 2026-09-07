using AddisMedConnect.Application.DTOs;

namespace AddisMedConnect.Application.Interfaces;

public interface IAuthService
{
    Task<AuthLoginResult> LoginAsync(LoginDto dto);
    Task<IEnumerable<UserManagementDto>> GetUsersAsync();
    Task<(bool Success, string? ErrorMessage, UserManagementDto? User)> RegisterUserAsync(CreateUserDto dto);
    Task<(bool Success, string? ErrorMessage, bool NotFound, UserManagementDto? User)> UpdateUserAsync(Guid id, UpdateUserDto dto);
    Task<(bool Success, string? ErrorMessage, bool NotFound)> DeleteUserAsync(Guid id, Guid currentUserId);
    Task<UserManagementDto?> UnlockUserAsync(Guid id);
}
