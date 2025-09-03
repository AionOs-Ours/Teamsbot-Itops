using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.Graph.Beta;
using Microsoft.Graph.Beta.Models;
using TeamsBot.Models;
using TeamsBot.Services.Interfaces;

namespace TeamsBot.Services
{
    public class IntuneService : IIntuneService
    {
        string tenantId = "13345921-c174-438f-9a21-1a76064a1a11";
        string clientId = "0b0dd3a1-d1da-4cea-b4a9-6f1ac5584454";
        string appId = "cdb5b3e7-d85a-4033-ac55-b764c519ef0f";           // Intune app ID (Win32LobApp/MSI/etc.)
        string clientSecret = "p648Q~9DGG32R_Q2TLE0gyn_AmtkeXzKQl8ARdij";
        public IntuneService()
        {

        }
        public async Task<string> GetAccessTokenAsync()
        {


            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

            return token.Token;

        }
        private async Task<string> assignScript(string scriptId, StringContent assignContent)
        {
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Token);

            var assignResponse=await httpClient.PostAsync(
                        $"https://graph.microsoft.com/beta/deviceManagement/deviceManagementScripts/{scriptId}/assign",
                        assignContent);

            if (assignResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"✅ Script assigned to group");
            }
            else
            {
                Console.WriteLine($"❌ Failed to assign script: {assignResponse.StatusCode}");
                Console.WriteLine(await assignResponse.Content.ReadAsStringAsync());
            }
            return "done";

        }
        private async Task<GraphModel> PushSoftware(string userId)
        {
            try
            {


                // 🔹 Replace with your values
         
                string deviceName = "AINHYDLP4359412";       // device to target


                // 🔹 Authenticate
                var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var token = await credential.GetTokenAsync(
           new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }));

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token.Token);
                var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });
                //var token = await GetAccessTokenAsync(); // must return a valid token

                //var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(request =>
                //{
                //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                //    return Task.CompletedTask;
                //}));


                // -------------------------------------------------------
                // 1️⃣ Find the device in Intune
                // -------------------------------------------------------
                var Alldevices = await graphClient.DeviceManagement.ManagedDevices.GetAsync();
                var device = Alldevices.Value.FirstOrDefault(x => x.UserId == userId);
                //var devices = await graphClient.DeviceManagement.ManagedDevices
                //.GetAsync(req =>
                //{
                //    req.QueryParameters.Filter = $"userId eq '{userId}'";
                //});

                if (device is null)
                {
                    Console.WriteLine($"❌ Device {deviceName} not found.");
                    //return;
                }


                Console.WriteLine($"✅ Found device {device.DeviceName}, Id = {device.AzureADDeviceId}");

                // -------------------------------------------------------
                // 2️⃣ Check if group already exists for this device
                // -------------------------------------------------------
                string groupPrefix = $"AppDeployment-{device.DeviceName}";
                Group existingGroup = null;

                var existingGroups = await graphClient.Groups
                    .GetAsync(req =>
                    {
                        req.QueryParameters.Filter = $"startsWith(displayName,'{groupPrefix}')";
                    });

                if (existingGroups?.Value?.Count > 0)
                {
                    existingGroup = existingGroups.Value[0];
                    Console.WriteLine($"🔄 Reusing existing group: {existingGroup.DisplayName}");
                }
                else
                {
                    // -------------------------------------------------------
                    // 3️⃣ Create new security group if none exists
                    // -------------------------------------------------------
                    string groupName = $"{groupPrefix}-{Guid.NewGuid()}";

                    var newGroup = new Group
                    {
                        DisplayName = groupName,
                        Description = $"Dynamic group for app deployment to {device.DeviceName}",
                        MailEnabled = false,
                        MailNickname = Guid.NewGuid().ToString(),
                        SecurityEnabled = true
                    };

                    existingGroup = await graphClient.Groups.PostAsync(newGroup);
                    Console.WriteLine($"✅ Created new group {existingGroup.DisplayName}, Id = {existingGroup.Id}");
                }

                // -------------------------------------------------------
                // 4️⃣ Add the device as a member (skip if already present)
                // -------------------------------------------------------
                bool alreadyMember = false;
                var members = await graphClient.Groups[existingGroup.Id].Members.GetAsync();

                if (members?.Value != null)
                {
                    foreach (var member in members.Value)
                    {
                        if (member.Id == device.UserId.ToString())
                        {
                            alreadyMember = true;
                            break;
                        }
                    }
                }

                if (!alreadyMember)
                {
                    await graphClient.Groups[existingGroup.Id].Members.Ref
                    .PostAsync(new ReferenceCreate
                    {
                        OdataId = $"https://graph.microsoft.com/v1.0/directoryObjects/{device.UserId}"
                    });

                }
                return new GraphModel
                {
                    graphServiceClient = graphClient,
                    utilityId = existingGroup.Id
                };

                //// -------------------------------------------------------
                //// 5️⃣ Assign the app to the group (skip if already assigned)
                //// -------------------------------------------------------
                //var assignments = await graphClient.DeviceAppManagement.MobileApps[appId].Assignments.GetAsync();
                //bool alreadyAssigned = false;

                //if (assignments?.Value != null)
                //{
                //    foreach (var assign in assignments.Value)
                //    {
                //        if (assign.Target is GroupAssignmentTarget groupTarget &&
                //            groupTarget.GroupId == existingGroup.Id)
                //        {
                //            alreadyAssigned = true;
                //            break;
                //        }
                //    }
                //}

                //if (!alreadyAssigned)
                //{
                //    var assignment = new MobileAppAssignment
                //    {
                //        Intent = InstallIntent.Required,
                //        Target = new GroupAssignmentTarget
                //        {
                //            GroupId = existingGroup.Id
                //        }
                //    };

                //    await graphClient.DeviceAppManagement.MobileApps[appId].Assignments
                //        .PostAsync(assignment);

                //    Console.WriteLine($"✅ Assigned app {appId} to group {existingGroup.DisplayName}");
                //}
                //else
                //{
                //    Console.WriteLine($"ℹ️ App already assigned to group {existingGroup.DisplayName}");
                //}
                //return "Done";
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<string> DeployApp(string userId)
        {
            string appId = "cdb5b3e7-d85a-4033-ac55-b764c519ef0f";           // Intune app ID (Win32LobApp/MSI/etc.)
            var graphModel = await PushSoftware(userId);
            // -------------------------------------------------------
            // 5️⃣ Assign the app to the group (skip if already assigned)
            // -------------------------------------------------------
            var assignments = await graphModel.graphServiceClient.DeviceAppManagement.MobileApps[appId].Assignments.GetAsync();
            bool alreadyAssigned = false;

            if (assignments?.Value != null)
            {
                foreach (var assign in assignments.Value)
                {
                    if (assign.Target is GroupAssignmentTarget groupTarget &&
                        groupTarget.GroupId == graphModel.utilityId)
                    {
                        alreadyAssigned = true;
                        break;
                    }
                }
            }

            if (!alreadyAssigned)
            {
                var assignment = new MobileAppAssignment
                {
                    Intent = InstallIntent.Required,
                    Target = new GroupAssignmentTarget
                    {
                        GroupId = graphModel.utilityId
                    }
                };

                await graphModel.graphServiceClient.DeviceAppManagement.MobileApps[appId].Assignments
                    .PostAsync(assignment);

            }
            return "Done";
        }
        public async Task<string> DeployScript(string userId, string scriptContent, string scriptName="PythonSuite.ps1")
        {
            try
            {


                var graphModel = await PushSoftware(userId);
                var scriptId = await CreateScript(graphModel.graphServiceClient, scriptContent, scriptName);
                // -------------------------------------------------------
                // 5️⃣ Assign the app to the group (skip if already assigned)
                // -------------------------------------------------------
                var assignments = new DeviceManagementScriptAssignment
                {
                    Target = new GroupAssignmentTarget
                    {
                        GroupId = graphModel.utilityId
                    }
                };
                var existingAssignments = await graphModel.graphServiceClient.DeviceManagement.DeviceManagementScripts[graphModel.utilityId]
               .Assignments
               .GetAsync();
                bool alreadyAssigned = false;

                if (existingAssignments?.Value != null)
                {
                    foreach (var assign in existingAssignments.Value)
                    {
                        if (assign.Target is GroupAssignmentTarget groupTarget &&
                            groupTarget.GroupId == graphModel.utilityId)
                        {
                            alreadyAssigned = true;
                            break;
                        }
                    }
                }

                if (!alreadyAssigned)
                {
                    var assignPayload = new
                    {
                        deviceManagementScriptAssignments = new[]
           {
                new {
                    target = new Dictionary<string, object>
            {
                { "@odata.type", "#microsoft.graph.groupAssignmentTarget" },
                { "groupId", graphModel.utilityId }
            }
                }
            }
                    };

                    var assignContent = new StringContent(JsonSerializer.Serialize(assignPayload), Encoding.UTF8, "application/json");
                    var response = await assignScript(scriptId, assignContent);
                }
                return "Done";
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        private async Task<string> CreateScript(GraphServiceClient graphClient, string scriptContent, string scriptName)
        {

            var scripts = await graphClient.DeviceManagement.DeviceManagementScripts
           .GetAsync();

            var existingScript = scripts?.Value?
                .FirstOrDefault(s => s.DisplayName == scriptName);

            if (existingScript != null)
            {
                Console.WriteLine($"⚠️ Script already exists with ID: {existingScript.Id}. Rewriting...");

                // Delete the old one to enforce rewrite
                await graphClient.DeviceManagement.DeviceManagementScripts[existingScript.Id]
                    .DeleteAsync();

                Console.WriteLine($"🗑️ Deleted existing script: {existingScript.Id}");
            }
            var script = new DeviceManagementScript
            {
                DisplayName = "My Intune Script",
                Description = "This script configures settings",
                ScriptContent = System.Text.Encoding.UTF8.GetBytes(scriptContent),
                RunAsAccount = RunAsAccountType.System,
                EnforceSignatureCheck = false,
                FileName = scriptName
            };

            var createdScript = await graphClient.DeviceManagement.DeviceManagementScripts
                .PostAsync(script);
            return createdScript.Id;
        }
    }


   
}
