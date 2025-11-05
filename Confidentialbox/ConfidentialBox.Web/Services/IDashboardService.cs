using ConfidentialBox.Core.DTOs;
using System.Threading.Tasks;

namespace ConfidentialBox.Web.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto?> GetStatsAsync();
}