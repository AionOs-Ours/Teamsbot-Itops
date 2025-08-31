using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TeamsBot.Services.Interfaces;
using static System.Net.WebRequestMethods;

namespace TeamsBot.Services
{


public class ServiceNowService: IServiceNowService
    {
        private readonly IConfiguration _config;
        private readonly string baseUrl;
        private readonly string username;
        private readonly string password;
        private readonly HttpClient client;
        private readonly Dictionary<string, string> statusMap = new()
    {
        { "1", "New" },
        { "2", "In Progress" },
        { "3", "On Hold" },
        { "6", "Resolved" },
        { "7", "Closed" },
        { "8", "Canceled" }
    };

        public ServiceNowService(IConfiguration config)
        {
            _config = config;
            username = "admin";// _config["Config.ServiceNowConfig.Username"];
            password = "MPx4dSx3$Bd+";// _config["Config.ServiceNowConfig.Password"]"";
            baseUrl= "https://dev356668.service-now.com/api/now/table/incident";
            client = new HttpClient();
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<dynamic> CreateIncidentAsync(string shortDescription, string description = "", string urgency = "3", string impact = "3", string category = "Software")
        {
            var payload = new
            {
                short_description = shortDescription,
                description,
                urgency,
                impact,
                category
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(baseUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<dynamic>(json).result;
            }

            throw new Exception($"Create failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<Dictionary<string, string>> CheckIncidentStatusAsync(string incidentId)
        {
            string url = incidentId.ToUpper().StartsWith("INC")
                ? $"{baseUrl}?sysparm_query=number={incidentId}"
                : $"{baseUrl}/{incidentId}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Status check failed: {response.StatusCode}");

            var json = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject<dynamic>(json).result;

            if (result == null || (result is Newtonsoft.Json.Linq.JArray arr && arr.Count == 0))
                return null;

            var incident = result is Newtonsoft.Json.Linq.JArray ? result[0] : result;
            return ProcessIncidentStatus(incident);
        }

        public async Task<dynamic> UpdateIncidentStatusAsync(string incidentId, string status, string message = null)
        {
            var payload = new Dictionary<string, string> { { "state", status } };
            if (!string.IsNullOrEmpty(message))
                payload["close_notes"] = message;

            string url;
            if (incidentId.ToUpper().StartsWith("INC"))
            {
                var lookupUrl = $"{baseUrl}?sysparm_query=number={incidentId}";
                var lookupResp = await client.GetAsync(lookupUrl);
                if (!lookupResp.IsSuccessStatusCode)
                    return null;

                var lookupJson = await lookupResp.Content.ReadAsStringAsync();
                dynamic lookupResult = JsonConvert.DeserializeObject<dynamic>(lookupJson).result;
                if (lookupResult == null || lookupResult.Count == 0)
                    return null;

                string sysId = lookupResult[0].sys_id;
                url = $"{baseUrl}/{sysId}";
            }
            else
            {
                url = $"{baseUrl}/{incidentId}";
            }

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<dynamic>(json).result;
            }

            return null;
        }

        private Dictionary<string, string> ProcessIncidentStatus(dynamic incident)
        {
            string state = incident.state ?? "";
            string statusName = statusMap.ContainsKey(state) ? statusMap[state] : $"Unknown ({state})";

            return new Dictionary<string, string>
        {
            { "incident_number", incident.number ?? "N/A" },
            { "sys_id", incident.sys_id ?? "N/A" },
            { "status_code", state },
            { "status_name", statusName },
            { "short_description", incident.short_description ?? "N/A" },
            { "assigned_to", GetAssignedTo(incident) },
            { "created_on", incident.sys_created_on ?? "N/A" },
            { "updated_on", incident.sys_updated_on ?? "N/A" },
            { "resolution_notes", incident.close_notes ?? "N/A" }
        };
        }

        private string GetAssignedTo(dynamic incident)
        {
            var assignedTo = incident.assigned_to;
            if (assignedTo is Newtonsoft.Json.Linq.JObject obj && obj["display_value"] != null)
                return obj["display_value"].ToString();

            return assignedTo?.ToString() ?? "Unassigned";
        }
    }

}
