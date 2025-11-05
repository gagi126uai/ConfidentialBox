using ConfidentialBox.Core.DTOs;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string?> GetTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}