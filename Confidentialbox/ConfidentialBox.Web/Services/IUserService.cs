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
    Task<UserDto?> UpdateUserAsync(string id, UpdateUserProfileRequest request);
    Task<UserProfileDto?> GetMyProfileAsync();
    Task<OperationResultDto> UpdateMyProfileAsync(SelfProfileUpdateRequest request);
    Task<OperationResultDto> ChangeMyPasswordAsync(ChangeOwnPasswordRequest request);
    Task<List<UserMessageDto>> GetMyMessagesAsync(bool includeArchived = false);
    Task MarkMyMessageAsReadAsync(int messageId);
    Task<OperationResultDto> ReplyToMessageAsync(int messageId, string body);
    Task ArchiveMessageAsync(int messageId);
    Task UnarchiveMessageAsync(int messageId);
    Task<OperationResultDto> SendMessageAsync(string userId, CreateUserMessageRequest request);
    Task<OperationResultDto> ChangeUserPasswordAsync(string id, ChangeUserPasswordRequest request);
}