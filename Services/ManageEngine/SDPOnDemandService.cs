using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;

namespace TeamsBot.Services.ManageEngine
{
    public class SDPOnDemandService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly string _accessToken;
        private readonly string _sdpApiUrl = "https://sdpondemand.manageengine.com/api/v3/requests";
        public SDPOnDemandService(string accessToken) {
            _accessToken = accessToken;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Zoho-oauthtoken {_accessToken}");
        }
        public async Task<string> CreateRequestAsync(string subject, string description, string requesterName, string priority = "High", string status = "Open")
        {
            var requestPayload = new
            {
                request = new
                {
                    subject,
                    description,
                    requester = new { name = requesterName },
                    priority = new { name = priority },
                    status = new { name = status }
                }
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_sdpApiUrl, content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                // Log or handle error
                return $"Request failed: {ex.Message}";
            }
        }
    }
}
