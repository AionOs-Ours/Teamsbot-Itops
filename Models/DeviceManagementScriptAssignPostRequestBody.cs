
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Graph.Beta.Models;


namespace TeamsBot.Models
{

    public class DeviceManagementScriptAssignPostRequestBody
    {
        [JsonPropertyName("deviceManagementScriptAssignments")]
        public List<DeviceManagementScriptAssignment> DeviceManagementScriptAssignments { get; set; }
    }
}
