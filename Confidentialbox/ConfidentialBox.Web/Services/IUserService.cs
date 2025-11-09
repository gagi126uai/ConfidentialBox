using ConfidentialBox.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<UserDto?> CreateUserAsync(CreateUserRequest request);
    Task<bool> ToggleActiveAsync(string id);
    Task<bool> UpdateRolesAsync(string id, List<string> roles);
    Task<OperationResultDto> DeleteUserAsync(string id);
}