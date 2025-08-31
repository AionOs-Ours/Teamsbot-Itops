using System.Collections.Generic;
using System.Threading.Tasks;

namespace TeamsBot.Services.Interfaces
{
    public interface IServiceNowService
    {
        Task<dynamic> CreateIncidentAsync(
            string shortDescription,
            string description = "",
            string urgency = "3",
            string impact = "3",
            string category = "Software"
        );

        Task<Dictionary<string, string>> CheckIncidentStatusAsync(string incidentId);

        Task<dynamic> UpdateIncidentStatusAsync(string incidentId, string status, string message = null);
    }
}
